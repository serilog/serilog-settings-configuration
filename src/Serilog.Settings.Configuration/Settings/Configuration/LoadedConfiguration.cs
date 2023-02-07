using Serilog.Core;

namespace Serilog.Settings.Configuration;

/// <summary>
/// Contains information about the loaded configuration.
/// </summary>
public class LoadedConfiguration
{
    readonly IDictionary<string, LoggingLevelSwitch> _logLevelSwitches;

    internal LoadedConfiguration(IDictionary<string, LoggingLevelSwitch> logLevelSwitches)
    {
        _logLevelSwitches = logLevelSwitches;
    }

    /// <summary>
    /// The log level switches that were loaded from the <c>LevelSwitches</c> section of the configuration.
    /// </summary>
    /// <remarks>The keys of the dictionary are the name of the switches without the leading <c>$</c> character.</remarks>
    public IReadOnlyDictionary<string, LoggingLevelSwitch> LogLevelSwitches => _logLevelSwitches.ToDictionary(e => e.Key.TrimStart('$'), e => e.Value);
}
