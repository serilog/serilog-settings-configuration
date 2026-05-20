using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using TestDummies;

namespace Serilog.Settings.Configuration.Tests;

static class DummyLoggerConfigurationExtensions
{
    public static LoggerConfiguration? DummyRollingFile(
        LoggerSinkConfiguration loggerSinkConfiguration,
        string pathFormat,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string? outputTemplate = null,
        IFormatProvider? formatProvider = null)
    {
        return null;
    }

    public static LoggerConfiguration? DummyRollingFile(
        LoggerSinkConfiguration loggerSinkConfiguration,
        ITextFormatter formatter,
        string pathFormat,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
    {
        return null;
    }

    public static LoggerConfiguration DummyParamsArray(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        params string[] values)
    {
        return loggerSinkConfiguration.Sink(new DummyParamsSink(values));
    }

    public static LoggerConfiguration DummyParamsEnumerable(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        params IEnumerable<string> values)
    {
        return loggerSinkConfiguration.Sink(new DummyParamsSink(values.ToArray()));
    }

    public static LoggerConfiguration DummyParamsList(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        params System.Collections.Generic.List<string> list)
    {
        return loggerSinkConfiguration.Sink(new DummyParamsSink(list.ToArray()));
    }

    public static LoggerConfiguration DummyParamsSpan(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        params ReadOnlySpan<string> values)
    {
        return loggerSinkConfiguration.Sink(new DummyParamsSink(values.ToArray()));
    }
}
