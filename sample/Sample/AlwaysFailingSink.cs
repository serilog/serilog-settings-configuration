using Serilog.Core;
using Serilog.Events;

namespace Sample;

public class AlwaysFailingSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        throw new InvalidOperationException(
            "AlwaysFailingSink always throws so the sample can demonstrate fallback/failure-listener behavior.");
    }
}
