using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Generic;
using TestDummies.Console;
using TestDummies.Console.Themes;

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
            List<Binding> objectBinding,
            string pathFormat,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return loggerSinkConfiguration.Sink(new DummyRollingFileSink(), restrictedToMinimumLevel);
        }

        public class Binding
        {
            public string Foo { get; set; }

            public string Abc { get; set; }
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

        public static LoggerConfiguration DummyArrayOfType(this LoggerDestructuringConfiguration loggerSinkConfiguration,
            List<Type> list,
            Type[] array = null,
            Type type = null,
            CustomCollection<Type> custom = null,
            CustomCollection<string> customString = null)
        {
            return loggerSinkConfiguration.With(DummyPolicy.Current = new DummyPolicy
            {
                List = list,
                Array = array,
                Type = type,
                Custom = custom,
                CustomStrings = customString,
            });
        }
    }
}
