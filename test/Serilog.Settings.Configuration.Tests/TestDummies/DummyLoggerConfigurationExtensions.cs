using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Settings.Configuration.Tests.TestDummies.Console;
using Serilog.Settings.Configuration.Tests.TestDummies.Console.Themes;

namespace Serilog.Settings.Configuration.Tests.TestDummies
{
    static class DummyLoggerConfigurationExtensions
    {
        public static LoggerConfiguration DummyRollingFile(
            LoggerSinkConfiguration loggerSinkConfiguration,
            string pathFormat,
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
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return null;
        }

        public static LoggerConfiguration DummyConsole(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            ConsoleTheme theme = null)
        {
            return loggerSinkConfiguration.Sink(new DummyConsoleSink(theme), restrictedToMinimumLevel);
        }
    }
}
