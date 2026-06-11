namespace HostMe.Host.Constants;

public static class SwaggerConstants
{
    public const string DocName = "v1";
    public const string Title = "HostMe API";
    public const string Version = "v1";
    public const string Description = "HostMe Backend API";

    public const string SecurityScheme = "Bearer";
    public const string SecurityHeaderName = "Authorization";
    public const string SecurityBearerFormat = "JWT";
    public const string SecurityDescription = "Enter JWT token";

    public const string JsonEndpoint = "/swagger/v1/swagger.json";
    public const string UiTitle = "HostMe API v1";
}
