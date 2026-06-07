using HostMe.Application;
using HostMe.Domain.Repositories;
using HostMe.Domain.Security;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Infrastructure.Security;
using HostMe.Persistance;
using HostMe.Persistance.Repositories;
using HostMe.Host.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostMe.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContext<HostMeDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<IUserService, UserService>();
        
        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    var response = ApiResponse<object>.Failure(errors);
                    return new BadRequestObjectResult(response);
                };
            });

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HostMeDbContext>();
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        app.MapControllers();
        app.Run();
    }
}