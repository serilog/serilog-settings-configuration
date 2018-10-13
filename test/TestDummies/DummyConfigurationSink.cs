using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace TestDummies
{
    public class DummyConfigurationSink : ILogEventSink
    {
        [ThreadStatic]
        static List<LogEvent> _emitted;

        [ThreadStatic]
        static IConfiguration _configuration;

        [ThreadStatic]
        static IConfigurationSection _configSection;

        public static List<LogEvent> Emitted => _emitted ?? (_emitted = new List<LogEvent>());

        public static IConfiguration Configuration => _configuration;

        public static IConfigurationSection ConfigSection => _configSection;


        public DummyConfigurationSink(IConfiguration configuration, IConfigurationSection configSection)
        {
            _configuration = configuration;
            _configSection = configSection;
        }

        public void Emit(LogEvent logEvent)
        {
            Emitted.Add(logEvent);
        }

        public static void Reset()
        {
            _emitted = null;
            _configuration = null;
            _configSection = null;
        }

    }
}
