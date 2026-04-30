using Serilog.Core;
using Serilog.Events;

namespace TestDummies;

public class DummyFailureListener : ILoggingFailureListener
{
    [ThreadStatic]
    static int _failureCount;

    public static int FailureCount => _failureCount;

    public void OnLoggingFailed(
        object sender,
        LoggingFailureKind kind,
        string message,
        IReadOnlyCollection<LogEvent>? events,
        Exception? exception)
    {
        _failureCount++;
    }

    public static void Reset()
    {
        _failureCount = 0;
    }
}
