using Serilog.Core;
using Serilog.Events;

namespace Sample;

// The filter syntax in the sample configuration file is
// processed by the Serilog.Filters.Expressions package.
public class CustomFilter : ILogEventFilter
{
    readonly LogEventLevel _levelFilter;

    public CustomFilter(LogEventLevel levelFilter = LogEventLevel.Information)
    {
        _levelFilter = levelFilter;
    }

    public bool IsEnabled(LogEvent logEvent)
    {
        return logEvent.Level >= _levelFilter;
    }
}
