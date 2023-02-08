using Serilog.Core;

namespace Serilog.Settings.Configuration;

/// <summary>
/// Contains information about the loaded configuration.
/// </summary>
public class LoadedConfiguration
{
    internal LoadedConfiguration(IReadOnlyDictionary<string, LoggingLevelSwitch> logLevelSwitches)
    {
        LogLevelSwitches = logLevelSwitches;
    }

    /// <summary>
    /// The log level switches that were loaded from the <c>LevelSwitches</c> section of the configuration.
    /// </summary>
    /// <remarks>The keys of the dictionary are the name of the switches, including the leading <c>$</c> character.</remarks>
    public IReadOnlyDictionary<string, LoggingLevelSwitch> LogLevelSwitches { get; }
}
