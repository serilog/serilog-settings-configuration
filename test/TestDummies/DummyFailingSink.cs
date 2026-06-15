using Serilog.Core;
using Serilog.Events;

namespace TestDummies;

public class DummyFailingSink : ILogEventSink
{
    [ThreadStatic]
    static int _emitAttempts;

    public static int EmitAttempts => _emitAttempts;

    public void Emit(LogEvent logEvent)
    {
        _emitAttempts++;
        throw new InvalidOperationException("DummyFailingSink always fails.");
    }

    public static void Reset()
    {
        _emitAttempts = 0;
    }
}
