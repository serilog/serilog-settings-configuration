using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Sample;

public class SampleFailureListener : ILoggingFailureListener
{
    public void OnLoggingFailed(
        object sender,
        LoggingFailureKind kind,
        string message,
        IReadOnlyCollection<LogEvent>? events,
        Exception? exception)
    {
        var exceptionDetail = exception is null
            ? string.Empty
            : $" — {exception.GetType().Name}: {exception.Message}";
        SelfLog.WriteLine(
            $"[SampleFailureListener] {kind} failure from {sender.GetType().Name}: {message} ({events?.Count ?? 0} events){exceptionDetail}");
    }
}
