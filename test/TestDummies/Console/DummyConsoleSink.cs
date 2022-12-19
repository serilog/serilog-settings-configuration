using Serilog.Core;
using Serilog.Events;
using TestDummies.Console.Themes;

namespace TestDummies.Console;

public class DummyConsoleSink : ILogEventSink
{
    public DummyConsoleSink(ConsoleTheme theme = null)
    {
        Theme = theme ?? ConsoleTheme.None;
    }

    [ThreadStatic]
    public static ConsoleTheme Theme;

    [ThreadStatic]
    static List<LogEvent> EmittedList;

    public static List<LogEvent> Emitted => EmittedList ?? (EmittedList = new List<LogEvent>());

    public void Emit(LogEvent logEvent)
    {
        Emitted.Add(logEvent);
    }
}

