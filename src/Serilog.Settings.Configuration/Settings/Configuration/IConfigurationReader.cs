using Serilog.Configuration;

namespace Serilog.Settings.Configuration
{
    interface IConfigurationReader : ILoggerSettings
    {
        void ApplySinks(LoggerSinkConfiguration loggerSinkConfiguration);
        void ApplyEnrichment(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration);
    }
}
