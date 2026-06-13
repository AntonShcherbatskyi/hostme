using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace HostMe.Host.Extensions;

public static class ParameterStoreExtensions
{
    public static WebApplicationBuilder AddParameterStore(this WebApplicationBuilder builder)
    {
        var env = builder.Environment.IsProduction() ? "prod" : "dev";

        builder.Configuration.AddSystemsManager($"/hostme/{env}", new AWSOptions
        {
            Region = RegionEndpoint.EUWest2,
        }, optional: true);
        
        return builder;
    }
}
