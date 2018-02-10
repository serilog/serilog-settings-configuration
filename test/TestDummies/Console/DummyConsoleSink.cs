using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;
using TestDummies.Console.Themes;

namespace TestDummies.Console
{
    public class DummyConsoleSink : ILogEventSink
    {
        public DummyConsoleSink(ConsoleTheme theme = null)
        {
            Theme = theme ?? ConsoleTheme.None;
        }

        [ThreadStatic]
        public static ConsoleTheme Theme;

        [ThreadStatic]
        // ReSharper disable ThreadStaticFieldHasInitializer
        public static List<LogEvent> Emitted = new List<LogEvent>();
        // ReSharper restore ThreadStaticFieldHasInitializer

        public void Emit(LogEvent logEvent)
        {
            Emitted.Add(logEvent);
        }
    }

}

