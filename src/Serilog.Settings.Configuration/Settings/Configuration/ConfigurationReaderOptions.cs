using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Serilog.Core;

namespace Serilog.Settings.Configuration;

/// <summary>
/// Options to adjust how the configuration object is processed.
/// </summary>
public sealed class ConfigurationReaderOptions
{
    /// <summary>
    /// Initialize a new instance of the <see cref="ConfigurationReaderOptions"/> class.
    /// </summary>
    /// <param name="assemblies">A collection of assemblies that contains sinks and other types.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="assemblies"/> argument is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="assemblies"/> argument is empty.</exception>
    public ConfigurationReaderOptions(params Assembly[] assemblies)
    {
        Assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        if (assemblies.Length == 0)
            throw new ArgumentException("The assemblies array must not be empty.", nameof(assemblies));
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="ConfigurationReaderOptions"/> class.
    /// </summary>
    /// <remarks>Prefer the constructor taking explicit assemblies: <see cref="ConfigurationReaderOptions(System.Reflection.Assembly[])"/>. It's the only one supporting single-file publishing.</remarks>
    public ConfigurationReaderOptions() : this(dependencyContext: null)
    {
    }

    /// <summary>
    /// Initialize a new instance of the <see cref="ConfigurationReaderOptions"/> class.
    /// </summary>
    /// <param name="dependencyContext">
    /// The dependency context from which sink/enricher packages can be located. If <see langword="null"/>, the platform default will be used.
    /// </param>
    /// <remarks>Prefer the constructor taking explicit assemblies: <see cref="ConfigurationReaderOptions(System.Reflection.Assembly[])"/>. It's the only one supporting single-file publishing.</remarks>
    public ConfigurationReaderOptions(DependencyContext? dependencyContext) => DependencyContext = dependencyContext;

    /// <summary>
    /// Initialize a new instance of the <see cref="ConfigurationReaderOptions"/> class.
    /// </summary>
    /// <param name="configurationAssemblySource">Defines how the package identifies assemblies to scan for sinks and other types.</param>
    /// <remarks>Prefer the constructor taking explicit assemblies: <see cref="ConfigurationReaderOptions(System.Reflection.Assembly[])"/>. It's the only one supporting single-file publishing.</remarks>
    public ConfigurationReaderOptions(ConfigurationAssemblySource configurationAssemblySource) => ConfigurationAssemblySource = configurationAssemblySource;

    /// <summary>
    /// The section name for section which contains a Serilog section. Defaults to <c>Serilog</c>.
    /// </summary>
    public string? SectionName { get; init; } = ConfigurationLoggerConfigurationExtensions.DefaultSectionName;

    /// <summary>
    /// The <see cref="IFormatProvider"/> used when converting strings to other object types. Defaults to the invariant culture.
    /// </summary>
    public IFormatProvider? FormatProvider { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Allows to use internal types for extension methods for sink configuration. Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowInternalTypes { get; init; }

    /// <summary>
    /// Allows to use internal extension methods for sink configuration. Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowInternalMethods { get; init; }

    /// <summary>
    /// Called when a log level switch is created while reading the configuration.
    /// Log level switches are created either from the <c>Serilog:LevelSwitches</c> section (declared switches) or the <c>Serilog:MinimumLevel:Override</c> section (minimum level override switches).
    /// <list type="bullet">
    ///   <item>For declared switches, the switch name includes the leading <c>$</c> character.</item>
    ///   <item>For minimum level override switches, the switch name is the (partial) namespace or type name of the override.</item>
    /// </list>
    /// </summary>
    public Action<string, LoggingLevelSwitch>? OnLevelSwitchCreated { get; init; }

    internal Assembly[]? Assemblies { get; }
    internal DependencyContext? DependencyContext { get; }
    internal ConfigurationAssemblySource? ConfigurationAssemblySource { get; }
}
