using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Xunit;

namespace Serilog.Settings.Configuration.Tests.Support
{
    static class ConfigurationReaderTestHelpers
    {
        public const string minimumLevelFlatTemplate = @"
{{
    'Serilog': {{
        'MinimumLevel': '{0}'
    }}
}}";
        public const string minimumLevelObjectTemplate = @"
{{
    'Serilog': {{
        'MinimumLevel': {{
            'Default': '{0}'
        }}
    }}
}}";
        public const string minimumLevelFlatKey = "Serilog:MinimumLevel";
        public const string minimumLevelObjectKey = "Serilog:MinimumLevel:Default";

        public static void AssertLogEventLevels(LoggerConfiguration loggerConfig, LogEventLevel expectedMinimumLevel)
        {
            var logger = loggerConfig.CreateLogger();

            var logEventValues = Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>();

            foreach (var logEvent in logEventValues)
            {
                if (logEvent < expectedMinimumLevel)
                {
                    Assert.False(logger.IsEnabled(logEvent),
                        $"The log level {logEvent} should be disabled as it's lower priority than the minimum level of {expectedMinimumLevel}.");
                }
                else
                {
                    Assert.True(logger.IsEnabled(logEvent),
                        $"The log level {logEvent} should be enabled as it's {(logEvent == expectedMinimumLevel ? "the same" : "higher")} priority {(logEvent == expectedMinimumLevel ? "as" : "than")} the minimum level of {expectedMinimumLevel}.");
                }
            }
        }

        // the naming is only to show priority as providers
        public static IConfigurationRoot GetConfigRoot(
            string appsettingsJsonLevel = null,
            string appsettingsDevelopmentJsonLevel = null,
            Dictionary<string, string> envVariables = null)
        {
            var configBuilder = new ConfigurationBuilder();

            configBuilder.AddJsonString(appsettingsJsonLevel            ?? "{}");
            configBuilder.AddJsonString(appsettingsDevelopmentJsonLevel ?? "{}");
            configBuilder.Add(new ReloadableConfigurationSource(envVariables ?? new Dictionary<string, string>()));

            return configBuilder.Build();
        }
    }
}
