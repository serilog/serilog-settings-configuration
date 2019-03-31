using System;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Configuration;
using Serilog.Core;
using TestDummies.Console;
using TestDummies.Console.Themes;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace TestDummies
{
    public static class DummyLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithDummyThreadId(this LoggerEnrichmentConfiguration enrich)
        {
            return enrich.With(new DummyThreadIdEnricher());
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = null,
            IFormatProvider formatProvider = null)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            ITextFormatter formatter,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyWithConfiguration(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IConfiguration appConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyConfigurationSink(appConfiguration, null), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyWithOptionalConfiguration(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IConfiguration appConfiguration = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyConfigurationSink(appConfiguration, null), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyWithConfigSection(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IConfigurationSection configurationSection,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyConfigurationSink(null, configurationSection), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            List<string> objectBinding,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string[] stringArrayBinding,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            int[] intArrayBinding,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyRollingFile(
            this LoggerAuditSinkConfiguration loggerSinkConfiguration,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = null,
            IFormatProvider formatProvider = null)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileAuditSink(), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyWithLevelSwitch(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch controlLevelSwitch = null)
        {
            return loggerSinkConfiguration.Sink(new DummyWithLevelSwitchSink(controlLevelSwitch), restrictedToMinimumLevel);
        }

        public static LoggerConfiguration DummyConsole(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null,
            ConsoleTheme theme = null)
        {
            return loggerSinkConfiguration.Sink(new DummyConsoleSink(theme), restrictedToMinimumLevel, levelSwitch);
        }

        public static LoggerConfiguration Dummy(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> wrappedSinkAction)
        {
            return LoggerSinkConfiguration.Wrap(
                loggerSinkConfiguration,
                s => new DummyWrappingSink(s),
                wrappedSinkAction);
        }

        public static LoggerConfiguration WithDummyHardCodedString(
            this LoggerDestructuringConfiguration loggerDestructuringConfiguration,
            string hardCodedString
        )
        {
            return loggerDestructuringConfiguration.With(new DummyHardCodedStringDestructuringPolicy(hardCodedString));
        }

        public static LoggerConfiguration DummyWithFormatterSink(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            ITextFormatter formatter)
        {
            return loggerSinkConfiguration.Sink(new DummyWithFormatterSink(formatter));
        }

    }
}
