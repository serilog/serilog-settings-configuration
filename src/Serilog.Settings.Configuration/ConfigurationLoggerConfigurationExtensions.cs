// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Serilog.Configuration;
using Serilog.Settings.Configuration;
using Serilog.Settings.Configuration.Assemblies;

namespace Serilog;

/// <summary>
/// Extends <see cref="LoggerConfiguration"/> with support for System.Configuration appSettings elements.
/// </summary>
public static class ConfigurationLoggerConfigurationExtensions
{
    /// <summary>
    /// Configuration section name required by this package.
    /// </summary>
    public const string DefaultSectionName = "Serilog";

    /// <summary>
    /// Reads logger settings from the provided configuration object using the provided section name. Generally this
    /// is preferable over the other method that takes a configuration section. Only this version will populate
    /// IConfiguration parameters on target methods.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="sectionName">A section name for section which contains a Serilog section.</param>
    /// <param name="dependencyContext">The dependency context from which sink/enricher packages can be located. If not supplied, the platform
    /// default will be used.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        string sectionName,
        DependencyContext? dependencyContext = null)
    {
        if (settingConfiguration == null) throw new ArgumentNullException(nameof(settingConfiguration));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (sectionName == null) throw new ArgumentNullException(nameof(sectionName));

        var readerOptions = new ConfigurationReaderOptions(dependencyContext) { SectionName = sectionName, FormatProvider = null };
        return Configuration(settingConfiguration, configuration, readerOptions);
    }

    /// <summary>
    /// Reads logger settings from the provided configuration object using the default section name. Generally this
    /// is preferable over the other method that takes a configuration section. Only this version will populate
    /// IConfiguration parameters on target methods.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="dependencyContext">The dependency context from which sink/enricher packages can be located. If not supplied, the platform
    /// default will be used.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        DependencyContext dependencyContext)
        => Configuration(settingConfiguration, configuration, DefaultSectionName, dependencyContext);

    /// <summary>
    /// Reads logger settings from the provided configuration section. Generally it is preferable to use the other
    /// extension method that takes the full configuration object.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configSection">The Serilog configuration section</param>
    /// <param name="dependencyContext">The dependency context from which sink/enricher packages can be located. If not supplied, the platform
    /// default will be used.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, string sectionName, DependencyContext dependencyContext) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    [RequiresAssemblyFiles(TrimWarningMessages.IncompatibleWithSingleFile)]
    public static LoggerConfiguration ConfigurationSection(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfigurationSection configSection,
        DependencyContext? dependencyContext = null)
    {
        if (settingConfiguration == null) throw new ArgumentNullException(nameof(settingConfiguration));
        if (configSection == null) throw new ArgumentNullException(nameof(configSection));

        var assemblyFinder = dependencyContext == null
            ? AssemblyFinder.Auto()
            : AssemblyFinder.ForDependencyContext(dependencyContext);

        return settingConfiguration.Settings(
            new ConfigurationReader(
                configSection,
                assemblyFinder,
                new ConfigurationReaderOptions { FormatProvider = null },
                configuration: null));
    }

    /// <summary>
    /// Reads logger settings from the provided configuration object using the provided section name. Generally this
    /// is preferable over the other method that takes a configuration section. Only this version will populate
    /// IConfiguration parameters on target methods.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="sectionName">A section name for section which contains a Serilog section.</param>
    /// <param name="configurationAssemblySource">Defines how the package identifies assemblies to scan for sinks and other types.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        string sectionName,
        ConfigurationAssemblySource configurationAssemblySource)
    {
        if (settingConfiguration == null) throw new ArgumentNullException(nameof(settingConfiguration));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (sectionName == null) throw new ArgumentNullException(nameof(sectionName));

        var readerOptions = new ConfigurationReaderOptions(configurationAssemblySource) { SectionName = sectionName, FormatProvider = null };
        return Configuration(settingConfiguration, configuration, readerOptions);
    }

    /// <summary>
    /// Reads logger settings from the provided configuration object using the default section name. Generally this
    /// is preferable over the other method that takes a configuration section. Only this version will populate
    /// IConfiguration parameters on target methods.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="configurationAssemblySource">Defines how the package identifies assemblies to scan for sinks and other types.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        ConfigurationAssemblySource configurationAssemblySource)
        => Configuration(settingConfiguration, configuration, DefaultSectionName, configurationAssemblySource);

    /// <summary>
    /// Reads logger settings from the provided configuration section. Generally it is preferable to use the other
    /// extension method that takes the full configuration object.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configSection">The Serilog configuration section</param>
    /// <param name="configurationAssemblySource">Defines how the package identifies assemblies to scan for sinks and other types.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, string sectionName, ConfigurationAssemblySource configurationAssemblySource) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration ConfigurationSection(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfigurationSection configSection,
        ConfigurationAssemblySource configurationAssemblySource)
    {
        if (settingConfiguration == null) throw new ArgumentNullException(nameof(settingConfiguration));
        if (configSection == null) throw new ArgumentNullException(nameof(configSection));

        var assemblyFinder = AssemblyFinder.ForSource(configurationAssemblySource);

        return settingConfiguration.Settings(new ConfigurationReader(configSection, assemblyFinder, new ConfigurationReaderOptions { FormatProvider = null }, configuration: null));
    }

    /// <summary>
    /// Reads logger settings from the provided configuration object using the provided section name.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="sectionName">A section name for section which contains a Serilog section.</param>
    /// <param name="assemblies">A collection of assemblies that contains sinks and other types.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        string sectionName,
        params Assembly[] assemblies)
    {
        if (settingConfiguration == null) throw new ArgumentNullException(nameof(settingConfiguration));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (sectionName == null) throw new ArgumentNullException(nameof(sectionName));

        var readerOptions = new ConfigurationReaderOptions(assemblies) { SectionName = sectionName, FormatProvider = null };
        return Configuration(settingConfiguration, configuration, readerOptions);
    }

    /// <summary>
    /// Reads logger settings from the provided configuration object using the default section name.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="assemblies">A collection of assemblies that contains sinks and other types.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [Obsolete("Use ReadFrom.Configuration(IConfiguration configuration, ConfigurationReaderOptions readerOptions) instead.")]
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        params Assembly[] assemblies)
        => Configuration(settingConfiguration, configuration, DefaultSectionName, assemblies);

    /// <summary>
    /// Reads logger settings from the provided configuration object using the specified context.
    /// </summary>
    /// <param name="settingConfiguration">Logger setting configuration.</param>
    /// <param name="configuration">A configuration object which contains a Serilog section.</param>
    /// <param name="readerOptions">Options to adjust how the configuration object is processed.</param>
    /// <returns>An object allowing configuration to continue.</returns>
    [RequiresUnreferencedCode(TrimWarningMessages.NotSupportedWhenTrimming)]
    [RequiresDynamicCode(TrimWarningMessages.NotSupportedInAot)]
    public static LoggerConfiguration Configuration(
        this LoggerSettingsConfiguration settingConfiguration,
        IConfiguration configuration,
        ConfigurationReaderOptions? readerOptions = null)
    {
        var configurationReader = readerOptions switch
        {
            { ConfigurationAssemblySource: {} } => GetConfigurationReader(configuration, readerOptions, readerOptions.ConfigurationAssemblySource.Value),
            { Assemblies: {} } => GetConfigurationReader(configuration, readerOptions, readerOptions.Assemblies),
            _ => GetConfigurationReader(configuration, readerOptions ?? new ConfigurationReaderOptions(), readerOptions?.DependencyContext),
        };
        return settingConfiguration.Settings(configurationReader);
    }

    [RequiresUnreferencedCode(TrimWarningMessages.UnboundedReflection)]
    [RequiresDynamicCode(TrimWarningMessages.CreatesArraysOfArbitraryTypes)]
    static ConfigurationReader GetConfigurationReader(IConfiguration configuration, ConfigurationReaderOptions readerOptions, DependencyContext? dependencyContext)
    {
        var assemblyFinder = dependencyContext == null ? AssemblyFinder.Auto() : AssemblyFinder.ForDependencyContext(dependencyContext);
        var section = string.IsNullOrWhiteSpace(readerOptions.SectionName) ? configuration : configuration.GetSection(readerOptions.SectionName);
        return new ConfigurationReader(section, assemblyFinder, readerOptions, configuration);
    }

    [RequiresUnreferencedCode(TrimWarningMessages.UnboundedReflection)]
    [RequiresDynamicCode(TrimWarningMessages.CreatesArraysOfArbitraryTypes)]
    static ConfigurationReader GetConfigurationReader(IConfiguration configuration, ConfigurationReaderOptions readerOptions, ConfigurationAssemblySource source)
    {
        var assemblyFinder = AssemblyFinder.ForSource(source);
        var section = string.IsNullOrWhiteSpace(readerOptions.SectionName) ? configuration : configuration.GetSection(readerOptions.SectionName);
        return new ConfigurationReader(section, assemblyFinder, readerOptions, configuration);
    }

    [RequiresUnreferencedCode(TrimWarningMessages.UnboundedReflection)]
    [RequiresDynamicCode(TrimWarningMessages.CreatesArraysOfArbitraryTypes)]
    static ConfigurationReader GetConfigurationReader(IConfiguration configuration, ConfigurationReaderOptions readerOptions, IReadOnlyCollection<Assembly> assemblies)
    {
        var section = string.IsNullOrWhiteSpace(readerOptions.SectionName) ? configuration : configuration.GetSection(readerOptions.SectionName);
        return new ConfigurationReader(section, assemblies, new ResolutionContext(configuration, readerOptions));
    }
}
