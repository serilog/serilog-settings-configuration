using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Settings.Configuration.Tests
{
    using System.Collections.Generic;

    static class DummyLoggerConfigurationWithMultipleMethodsExtensions
    {
        public static LoggerConfiguration DummyRollingFile(
            LoggerSinkConfiguration loggerSinkConfiguration,
            ITextFormatter formatter,
            IEnumerable<string> pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = null,
            IFormatProvider formatProvider = null)
        {
            return null;
        }

        public static LoggerConfiguration DummyRollingFile(
            LoggerSinkConfiguration loggerSinkConfiguration,
            ITextFormatter formatter,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = null,
            IFormatProvider formatProvider = null)
        {
            return null;
        }

        public static LoggerConfiguration DummyRollingFile(
            LoggerSinkConfiguration loggerSinkConfiguration,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return null;
        }
    }
}
