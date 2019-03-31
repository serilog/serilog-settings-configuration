using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace TestDummies
{
    public class DummyWithFormatterSink : ILogEventSink
    {
        [ThreadStatic]
        static List<LogEvent> _emitted;

        [ThreadStatic]
        public static ITextFormatter Formatter;

        public DummyWithFormatterSink(ITextFormatter formatter)
        {
            Formatter = formatter;
        }

        public static List<LogEvent> Emitted => _emitted ?? (_emitted = new List<LogEvent>());

        public void Emit(LogEvent logEvent)
        {
            Emitted.Add(logEvent);
        }

        public static void Reset()
        {
            _emitted = null;
        }
    }
}
