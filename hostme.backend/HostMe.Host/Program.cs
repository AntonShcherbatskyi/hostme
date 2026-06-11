using System.Text;
using HostMe.Application;
using HostMe.Domain.Repositories;
using HostMe.Domain.Security;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Infrastructure.Options;
using HostMe.Infrastructure.Security;
using HostMe.Infrastructure.Storage;
using HostMe.Persistance;
using HostMe.Persistance.Repositories;
using HostMe.Host.Extensions;
using HostMe.Host.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HostMe.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<S3Options>()
            .BindConfiguration(S3Options.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddDbContext<HostMeDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.DefaultConnection);
        });

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISiteRepository, SiteRepository>();
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ISiteService, SiteService>();
        builder.Services.AddScoped<IS3Service, S3Service>();
        builder.Services.AddSingleton<ITempDirectoryFactory, TempDirectoryFactory>();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtSettingsOptions) =>
            {
                var jwtSettings = jwtSettingsOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret))
                };
            });

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        builder.Services.AddAuthorization();

        builder.Services.AddConfiguredCors(builder.Configuration);

        if (builder.Environment.IsDevelopment())
            builder.Services.AddSwaggerWithAuth();

        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return new BadRequestObjectResult(ApiResponse<object>.Failure(errors));
                };
            });

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSwaggerIfDevelopment();
        app.UseCors(CorsExtensions.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.ApplyMigrations();
        app.Run();
    }
}
