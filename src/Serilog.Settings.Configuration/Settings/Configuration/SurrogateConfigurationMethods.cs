using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Settings.Configuration
{
    /// <summary>
    /// Contains "fake extension" methods for the Serilog configuration API.
    /// By default the settings knows how to find extension methods, but some configuration
    /// are actually "regular" method calls and would not be found otherwise.
    ///
    /// This static class contains internal methods that can be used instead.
    ///
    /// </summary>
    static class SurrogateConfigurationMethods
    {
        public static IEnumerable<MethodInfo> WriteTo
        {
            get
            {
                yield return GetSurrogateConfigurationMethod<LoggerSinkConfiguration, Action<LoggerConfiguration>, LoggingLevelSwitch>((c, a, s) => Logger(c, a, LevelAlias.Minimum, s));
                yield return GetSurrogateConfigurationMethod<LoggerSinkConfiguration, ILogEventSink, LoggingLevelSwitch>((c, sink, s) => Sink(c, sink, LevelAlias.Minimum, s));
            }
        }

        public static IEnumerable<MethodInfo> Filter
        {
            get
            {
                yield return GetSurrogateConfigurationMethod<LoggerFilterConfiguration, ILogEventFilter, object>((c, f, _) => With(c, f));
            }
        }

        public static IEnumerable<MethodInfo> Destructure
        {
            get
            {
                yield return GetSurrogateConfigurationMethod<LoggerDestructuringConfiguration, IDestructuringPolicy, object>((c, d, _) => With(c, d));
                yield return GetSurrogateConfigurationMethod<LoggerDestructuringConfiguration, int, object>((c, m, _) => ToMaximumDepth(c, m));
                yield return GetSurrogateConfigurationMethod<LoggerDestructuringConfiguration, int, object>((c, m, _) => ToMaximumStringLength(c, m));
                yield return GetSurrogateConfigurationMethod<LoggerDestructuringConfiguration, int, object>((c, m, _) => ToMaximumCollectionCount(c, m));
                yield return GetSurrogateConfigurationMethod<LoggerDestructuringConfiguration, Type, object>((c, t, _) => AsScalar(c, t));
            }
        }

        public static IEnumerable<MethodInfo> Enrich
        {
            get
            {
                yield return GetSurrogateConfigurationMethod<LoggerEnrichmentConfiguration, object, object>((c, _, __) => FromLogContext(c));
            }
        }

        static MethodInfo GetSurrogateConfigurationMethod<TConfiguration, TArg1, TArg2>(Expression<Action<TConfiguration, TArg1, TArg2>> method)
            => (method.Body as MethodCallExpression)?.Method;

        /*
        Pass-through calls to various Serilog config methods which are
        implemented as instance methods rather than extension methods. The
        FindXXXConfigurationMethods calls (above) use these to add method
        invocation expressions as surrogates so that SelectConfigurationMethod
        has a way to match and invoke these instance methods.
        */

        internal static LoggerConfiguration Sink(
            LoggerSinkConfiguration loggerSinkConfiguration,
            ILogEventSink sink,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
        {
            return loggerSinkConfiguration.Sink(sink, restrictedToMinimumLevel, levelSwitch);
        }

        // TODO: add overload for array argument (ILogEventEnricher[])
        static LoggerConfiguration With(LoggerFilterConfiguration loggerFilterConfiguration, ILogEventFilter filter)
            => loggerFilterConfiguration.With(filter);

        // TODO: add overload for array argument (IDestructuringPolicy[])
        static LoggerConfiguration With(LoggerDestructuringConfiguration loggerDestructuringConfiguration, IDestructuringPolicy policy)
            => loggerDestructuringConfiguration.With(policy);

        static LoggerConfiguration ToMaximumDepth(LoggerDestructuringConfiguration loggerDestructuringConfiguration, int maximumDestructuringDepth)
            => loggerDestructuringConfiguration.ToMaximumDepth(maximumDestructuringDepth);

        static LoggerConfiguration ToMaximumStringLength(LoggerDestructuringConfiguration loggerDestructuringConfiguration, int maximumStringLength)
            => loggerDestructuringConfiguration.ToMaximumStringLength(maximumStringLength);

        static LoggerConfiguration ToMaximumCollectionCount(LoggerDestructuringConfiguration loggerDestructuringConfiguration, int maximumCollectionCount)
            => loggerDestructuringConfiguration.ToMaximumCollectionCount(maximumCollectionCount);

        static LoggerConfiguration AsScalar(LoggerDestructuringConfiguration loggerDestructuringConfiguration, Type scalarType)
            => loggerDestructuringConfiguration.AsScalar(scalarType);

        static LoggerConfiguration FromLogContext(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
            => loggerEnrichmentConfiguration.FromLogContext();

        // Unlike the other configuration methods, Logger is an instance method rather than an extension.
        static LoggerConfiguration Logger(
            LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerConfiguration> configureLogger,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
            => loggerSinkConfiguration.Logger(configureLogger, restrictedToMinimumLevel, levelSwitch);
    }
}
