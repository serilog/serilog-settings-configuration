using Serilog.Core;
using Serilog.Events;

namespace TestDummies;

public class DummyParamsSink : ILogEventSink
{
    public static string[]? LastValues { get; private set; }

    public DummyParamsSink(params string[] values)
    {
        LastValues = values;
    }

    public void Emit(LogEvent logEvent) { }

    public static void Reset()
    {
        LastValues = null;
    }
}
