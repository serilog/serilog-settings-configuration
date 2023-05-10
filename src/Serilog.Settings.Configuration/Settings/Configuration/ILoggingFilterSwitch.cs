namespace Serilog.Settings.Configuration;

/// <summary>
/// A log event filter that can be modified at runtime.
/// </summary>
/// <remarks>
/// Under the hood, the logging filter switch is either a <c>Serilog.Expressions.LoggingFilterSwitch</c> or a <c>Serilog.Filters.Expressions.LoggingFilterSwitch</c> instance.
/// </remarks>
public interface ILoggingFilterSwitch
{
    /// <summary>
    /// A filter expression against which log events will be tested.
    /// Only expressions that evaluate to <c>true</c> are included by the filter. A <c>null</c> expression will accept all events.
    /// </summary>
    public string? Expression { get; set; }
}
