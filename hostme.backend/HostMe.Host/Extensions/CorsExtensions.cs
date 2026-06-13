using HostMe.Domain.Constants;

namespace HostMe.Host.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AllowFrontend";
    
    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration
            .GetSection(ConfigurationKeys.CorsAllowedOrigins)
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
