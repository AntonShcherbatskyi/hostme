using HostMe.Host.Constants;
using Microsoft.OpenApi;

namespace HostMe.Host.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SwaggerConstants.DocName, new OpenApiInfo
            {
                Title = SwaggerConstants.Title,
                Version = SwaggerConstants.Version,
                Description = SwaggerConstants.Description
            });

            options.AddSecurityDefinition(SwaggerConstants.SecurityScheme, new OpenApiSecurityScheme
            {
                Name = SwaggerConstants.SecurityHeaderName,
                Type = SecuritySchemeType.Http,
                Scheme = SwaggerConstants.SecurityScheme,
                BearerFormat = SwaggerConstants.SecurityBearerFormat,
                In = ParameterLocation.Header,
                Description = SwaggerConstants.SecurityDescription
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SwaggerConstants.SecurityScheme, document)] =
                    new List<string>()
            });
        });

        return services;
    }
    
    public static WebApplication UseSwaggerIfDevelopment(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint(SwaggerConstants.JsonEndpoint, SwaggerConstants.UiTitle);
        });

        return app;
    }
}
