using Microsoft.Extensions.Configuration;

namespace Serilog.Settings.Configuration.Tests.Support
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddJsonString(this IConfigurationBuilder builder, string json)
        {
            return builder.Add(new JsonStringConfigSource(json));
        }
    }
}