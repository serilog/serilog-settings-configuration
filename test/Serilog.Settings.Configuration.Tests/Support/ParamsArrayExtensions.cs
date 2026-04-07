using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Settings.Configuration.Tests.Support
{
    internal static class ParamsArrayExtensions
    {
        public static LoggerConfiguration WithParamsArray(this LoggerEnrichmentConfiguration configuration, params string[] values)
        {
            return configuration.With(new ParamsArrayEnricher(values));
        }
    }

    public class ParamsArrayEnricher : ILogEventEnricher
    {
        public static string[]? LastValues { get; set; }
        public ParamsArrayEnricher(string[] values)
        {
            LastValues = values;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
    }
}
