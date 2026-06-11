using HostMe.Domain.Services;
using HostMe.Infrastructure.Storage;
using HostMe.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HostMe.Host.Tests;

public class HostMeWebApplicationFactory : WebApplicationFactory<Program>
{
    public FileSystemS3Service FakeS3 { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "super_secret_key_that_is_at_least_32_characters_long" },
                { "Jwt:Issuer", "HostMe" },
                { "Jwt:Audience", "HostMe" },
                { "Jwt:ExpiryMinutes", "60" },
                { "S3:BucketName", "hostme-test-bucket" },
                { "ConnectionStrings:DefaultConnection", "Server=dummy;Database=dummy" }
            });
        });

        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d => 
                d.ServiceType == typeof(DbContextOptions<HostMeDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(HostMeDbContext) ||
                (d.ServiceType.Namespace != null && 
                 (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") || 
                  d.ServiceType.Namespace.StartsWith("Npgsql.EntityFrameworkCore")))
            ).ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            var databaseName = "HostMeIntegrationTestsDb_" + Guid.NewGuid();
            services.AddDbContext<HostMeDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            var s3Descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IS3Service));
            if (s3Descriptor != null)
            {
                services.Remove(s3Descriptor);
            }
            services.AddSingleton<IS3Service>(FakeS3);
        });
    }
}
