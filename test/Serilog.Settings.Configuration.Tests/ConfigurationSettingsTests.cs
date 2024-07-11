using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;
using TestDummies;
using TestDummies.Console;
using TestDummies.Console.Themes;

namespace Serilog.Settings.Configuration.Tests;

public class ConfigurationSettingsTests
{
    static LoggerConfiguration ConfigFromJson(string jsonString, string? secondJsonSource = null, ConfigurationReaderOptions? options = null)
    {
        return ConfigFromJson(jsonString, secondJsonSource, out _, options);
    }

    static LoggerConfiguration ConfigFromJson(string jsonString, out IConfiguration configuration, ConfigurationReaderOptions? options = null)
    {
        return ConfigFromJson(jsonString, null, out configuration, options);
    }

    static LoggerConfiguration ConfigFromJson(string jsonString, string? secondJsonSource, out IConfiguration configuration, ConfigurationReaderOptions? options)
    {
        var builder = new ConfigurationBuilder().AddJsonString(jsonString);
        if (secondJsonSource != null)
            builder.AddJsonString(secondJsonSource);
        configuration = builder.Build();
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, options);
    }

    [Fact]
    public void PropertyEnrichmentIsApplied()
    {
        LogEvent? evt = null;

        // language=json
        var json = """
            {
                "Serilog": {
                    "Properties": {
                        "App": "Test"
                    }
                }
            }
            """;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Information("Has a test property");

        Assert.NotNull(evt);
        Assert.Equal("Test", evt.Properties["App"].LiteralValue());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CanReadWithoutSerilogSection(string? sectionName)
    {
        LogEvent? evt = null;

        // language=json
        var json = """
            {
                "Properties": {
                    "App": "Test"
                }
            }
            """;

        var log = ConfigFromJson(json, options: new ConfigurationReaderOptions { SectionName = sectionName })
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Information("Has a test property");

        Assert.NotNull(evt);
        Assert.Equal("Test", evt.Properties["App"].LiteralValue());
    }

    [Theory]
    [InlineData("extended syntax", """
    {
        "Serilog": {
            "Using": ["TestDummies"],
            "WriteTo": [
                { "Name": "DummyConsole"},
                { "Name": "DummyWithLevelSwitch"},
            ]
        }
    }
    """)]
    [InlineData("simplified syntax", """
    {
        "Serilog": {
            "Using": ["TestDummies"],
            "WriteTo": ["DummyConsole", "DummyWithLevelSwitch" ]
        }
    }
    """)]
    public void ParameterlessSinksAreConfigured(string syntax, string json)
    {
        _ = syntax;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyConsoleSink.Emitted.Clear();
        DummyWithLevelSwitchSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyConsoleSink.Emitted);
        Assert.Single(DummyWithLevelSwitchSink.Emitted);
    }

    [Fact]
    public void ConfigurationAssembliesFromDllScanning()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": ["DummyConsole"]
                }
            }
            """;

        var builder = new ConfigurationBuilder().AddJsonString(json);
        var config = builder.Build();
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(
                configuration: config,
                readerOptions: new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles))
            .CreateLogger();

        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyConsoleSink.Emitted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigurationAssembliesWithInternalMethodInPublicClass(bool allowInternalMethods)
    {
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": ["DummyConsoleInternal"]
                }
            }
            """;

        var builder = new ConfigurationBuilder().AddJsonString(json);
        var config = builder.Build();
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(
                configuration: config,
                readerOptions: new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles) { AllowInternalMethods = allowInternalMethods })
            .CreateLogger();

        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        if (allowInternalMethods)
            Assert.Single(DummyConsoleSink.Emitted);
        else
            Assert.Empty(DummyConsoleSink.Emitted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigurationAssembliesWithPublicMethodInInternalClass(bool allowInternalTypes)
    {
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": ["DummyConsolePublicInInternal"]
                }
            }
            """;

        var builder = new ConfigurationBuilder().AddJsonString(json);
        var config = builder.Build();
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(
                configuration: config,
                readerOptions: new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles) { AllowInternalTypes = allowInternalTypes })
            .CreateLogger();

        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        if (allowInternalTypes)
            Assert.Single(DummyConsoleSink.Emitted);
        else
            Assert.Empty(DummyConsoleSink.Emitted);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ConfigurationAssembliesWithInternalMethodInInternalClass(bool allowInternalTypes, bool allowInternalMethods)
    {
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": ["DummyConsoleInternalInInternal"]
                }
            }
            """;

        var builder = new ConfigurationBuilder().AddJsonString(json);
        var config = builder.Build();
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(
                configuration: config,
                readerOptions: new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles) { AllowInternalTypes = allowInternalTypes, AllowInternalMethods = allowInternalMethods })
            .CreateLogger();

        DummyConsoleSink.Emitted.Clear();

        log.Write(Some.InformationEvent());

        if (allowInternalTypes && allowInternalMethods)
            Assert.Single(DummyConsoleSink.Emitted);
        else
            Assert.Empty(DummyConsoleSink.Emitted);
    }

    [Fact]
    public void SinksAreConfigured()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\"}
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        DummyRollingFileAuditSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
        Assert.Empty(DummyRollingFileAuditSink.Emitted);
    }

    [Fact]
    public void AuditSinksAreConfigured()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "AuditTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\"}
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        DummyRollingFileAuditSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Empty(DummyRollingFileSink.Emitted);
        Assert.Single(DummyRollingFileAuditSink.Emitted);
    }

    [Fact]
    public void AuditToSubLoggersAreConfigured()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "AuditTo": [{
                        "Name": "Logger",
                        "Args": {
                            "configureLogger" : {
                                "AuditTo": [{
                                    "Name": "DummyRollingFile",
                                    "Args": {"pathFormat" : "C:\\"}
                                }]}
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        DummyRollingFileAuditSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Empty(DummyRollingFileSink.Emitted);
        Assert.Single(DummyRollingFileAuditSink.Emitted);
    }

    [Fact]
    public void TestMinimumLevelOverrides()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "MinimumLevel" : {
                        "Override" : {
                            "System" : "Warning"
                        }
                    }
                }
            }
            """;

        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        var systemLogger = log.ForContext<WeakReference>();
        systemLogger.Write(Some.InformationEvent());

        Assert.Null(evt);

        systemLogger.Warning("Bad things");
        Assert.NotNull(evt);

        evt = null;
        log.Write(Some.InformationEvent());
        Assert.NotNull(evt);
    }

    [Fact]
    public void TestMinimumLevelOverridesForChildContext()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "MinimumLevel" : {
                        "Default" : "Warning",
                        "Override" : {
                            "System" : "Warning",
                            "System.Threading": "Debug"
                        }
                    }
                }
            }
            """;

        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Write(Some.DebugEvent());
        Assert.Null(evt);

        var custom = log.ForContext(Constants.SourceContextPropertyName, typeof(Task).FullName + "<42>");
        custom.Write(Some.DebugEvent());
        Assert.NotNull(evt);

        evt = null;
        var systemThreadingLogger = log.ForContext<Task>();
        systemThreadingLogger.Write(Some.DebugEvent());
        Assert.NotNull(evt);
    }

    [Fact]
    public void SinksWithAbstractParamsAreConfiguredWithTypeName()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyConsole",
                        "Args": {"theme" : "Serilog.Settings.Configuration.Tests.Support.CustomConsoleTheme, Serilog.Settings.Configuration.Tests"}
                    }]
                }
            }
            """;

        DummyConsoleSink.Theme = null;

        ConfigFromJson(json)
            .CreateLogger();

        Assert.NotNull(DummyConsoleSink.Theme);
        Assert.IsType<CustomConsoleTheme>(DummyConsoleSink.Theme);
    }

    [Fact]
    public void SinksAreConfiguredWithStaticMember()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyConsole",
                        "Args": {"theme" : "TestDummies.Console.Themes.ConsoleThemes::Theme1, TestDummies"}
                    }]
                }
            }
            """;

        DummyConsoleSink.Theme = null;

        ConfigFromJson(json)
            .CreateLogger();

        Assert.Equal(ConsoleThemes.Theme1, DummyConsoleSink.Theme);
    }

    [Theory]
    [InlineData("$switchName", true)]
    [InlineData("$SwitchName", true)]
    [InlineData("SwitchName", true)]
    [InlineData("$switch1", true)]
    [InlineData("$sw1tch0", true)]
    [InlineData("sw1tch0", true)]
    [InlineData("$SWITCHNAME", true)]
    [InlineData("$$switchname", false)]
    [InlineData("$switchname$", false)]
    [InlineData("switch$name", false)]
    [InlineData("$", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("$1switch", false)]
    [InlineData("$switch_name", false)]
    public void LoggingLevelSwitchNameValidityScenarios(string switchName, bool expectedValid)
    {
        Assert.True(ConfigurationReader.IsValidSwitchName(switchName) == expectedValid,
            $"expected IsValidSwitchName({switchName}) to return {expectedValid} ");
    }

    [Fact]
    public void LoggingLevelSwitchWithInvalidNameThrowsFormatException()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "LevelSwitches": {"1InvalidSwitchName" : "Warning" }
                }
            }
            """;

        var ex = Assert.Throws<FormatException>(() => ConfigFromJson(json));

        Assert.Contains("\"1InvalidSwitchName\"", ex.Message);
        Assert.Contains("'$' sign", ex.Message);
        Assert.Contains("\"LevelSwitches\" : {\"$switchName\" :", ex.Message);
    }

    [Theory]
    [InlineData("$mySwitch")]
    [InlineData("mySwitch")]
    public void LoggingFilterSwitchIsConfigured(string switchName)
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "FilterSwitches": { "{{switchName}}": "Prop = 42" },
                    "Filter:BySwitch": {
                        "Name": "ControlledBy",
                        "Args": {
                            "switch": "$mySwitch"
                        }
                    }
                }
            }
            """;
        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Write(Some.InformationEvent());
        Assert.Null(evt);

        log.ForContext("Prop", 42).Write(Some.InformationEvent());
        Assert.NotNull(evt);
    }

    [Theory]
    [InlineData("$switch1")]
    [InlineData("switch1")]
    public void LoggingLevelSwitchIsConfigured(string switchName)
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "LevelSwitches": { "{{switchName}}" : "Warning" },
                    "MinimumLevel" : {
                        "ControlledBy" : "$switch1"
                    }
                }
            }
            """;
        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Write(Some.DebugEvent());
        Assert.True(evt is null, "LoggingLevelSwitch initial level was Warning. It should not log Debug messages");
        log.Write(Some.InformationEvent());
        Assert.True(evt is null, "LoggingLevelSwitch initial level was Warning. It should not log Information messages");
        log.Write(Some.WarningEvent());
        Assert.True(evt != null, "LoggingLevelSwitch initial level was Warning. It should log Warning messages");
    }

    [Fact]
    public void SettingMinimumLevelControlledByToAnUndeclaredSwitchThrows()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "LevelSwitches": {"$switch1" : "Warning" },
                    "MinimumLevel" : {
                        "ControlledBy" : "$switch2"
                    }
                }
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigFromJson(json)
                .CreateLogger());

        Assert.Contains("$switch2", ex.Message);
        Assert.Contains("\"LevelSwitches\":{\"$switch2\":", ex.Message);
    }

    [Fact]
    public void LoggingLevelSwitchIsPassedToSinks()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "LevelSwitches": {"$switch1" : "Information" },
                    "MinimumLevel" : {
                        "ControlledBy" : "$switch1"
                    },
                    "WriteTo": [{
                        "Name": "DummyWithLevelSwitch",
                        "Args": {"controlLevelSwitch" : "$switch1"}
                    }]
                }
            }
            """;

        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        Assert.False(DummyWithLevelSwitchSink.ControlLevelSwitch == null, "Sink ControlLevelSwitch should have been initialized");

        var controlSwitch = DummyWithLevelSwitchSink.ControlLevelSwitch;
        Assert.NotNull(controlSwitch);

        log.Write(Some.DebugEvent());
        Assert.True(evt is null, "LoggingLevelSwitch initial level was information. It should not log Debug messages");

        controlSwitch.MinimumLevel = LogEventLevel.Debug;
        log.Write(Some.DebugEvent());
        Assert.True(evt != null, "LoggingLevelSwitch level was changed to Debug. It should log Debug messages");
    }

    [Fact]
    public void ReferencingAnUndeclaredSwitchInSinkThrows()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "LevelSwitches": {"$switch1" : "Information" },
                    "MinimumLevel" : {
                        "ControlledBy" : "$switch1"
                    },
                    "WriteTo": [{
                        "Name": "DummyWithLevelSwitch",
                        "Args": {"controlLevelSwitch" : "$switch2"}
                    }]
                }
            }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigFromJson(json)
                .CreateLogger());

        Assert.Contains("$switch2", ex.Message);
        Assert.Contains("\"LevelSwitches\":{\"$switch2\":", ex.Message);
    }

    [Fact]
    public void LoggingLevelSwitchCanBeUsedForMinimumLevelOverrides()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "LevelSwitches": {"$specificSwitch" : "Warning" },
                    "MinimumLevel" : {
                        "Default" : "Debug",
                        "Override" : {
                            "System" : "$specificSwitch"
                        }
                    },
                    "WriteTo": [{
                        "Name": "DummyWithLevelSwitch",
                        "Args": {"controlLevelSwitch" : "$specificSwitch"}
                    }]
                }
            }
            """;

        LogEvent? evt = null;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        var systemLogger = log.ForContext(Constants.SourceContextPropertyName, "System.Bar");

        log.Write(Some.InformationEvent());
        Assert.False(evt is null, "Minimum level is Debug. It should log Information messages");

        evt = null;
        // ReSharper disable HeuristicUnreachableCode
        systemLogger.Write(Some.InformationEvent());
        Assert.True(evt is null, "LoggingLevelSwitch initial level was Warning for logger System.*. It should not log Information messages for SourceContext System.Bar");

        systemLogger.Write(Some.WarningEvent());
        Assert.False(evt is null, "LoggingLevelSwitch initial level was Warning for logger System.*. It should log Warning messages for SourceContext System.Bar");


        evt = null;
        var controlSwitch = DummyWithLevelSwitchSink.ControlLevelSwitch;
        Assert.NotNull(controlSwitch);

        controlSwitch.MinimumLevel = LogEventLevel.Information;
        systemLogger.Write(Some.InformationEvent());
        Assert.False(evt is null, "LoggingLevelSwitch level was changed to Information for logger System.*. It should now log Information events for SourceContext System.Bar.");
        // ReSharper restore HeuristicUnreachableCode
    }

    [Fact]

    [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/142")]
    public void SinkWithIConfigurationArguments()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyWithConfiguration",
                        "Args": {}
                    }]
                }
            }
            """;

        DummyConfigurationSink.Reset();
        var log = ConfigFromJson(json, out var expectedConfig)
            .CreateLogger();

        log.Write(Some.InformationEvent());

        Assert.NotNull(DummyConfigurationSink.Configuration);
        Assert.Same(expectedConfig, DummyConfigurationSink.Configuration);
    }

    [Fact]
    [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/142")]
    public void SinkWithOptionalIConfigurationArguments()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyWithOptionalConfiguration",
                        "Args": {}
                    }]
                }
            }
            """;

        DummyConfigurationSink.Reset();
        var log = ConfigFromJson(json, out var expectedConfig)
            .CreateLogger();

        log.Write(Some.InformationEvent());

        // null is the default value, but we have a configuration to provide
        Assert.NotNull(DummyConfigurationSink.Configuration);
        Assert.Same(expectedConfig, DummyConfigurationSink.Configuration);
    }

    [Fact]
    public void SinkWithIConfigSectionArguments()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyWithConfigSection",
                        "Args": {"configurationSection" : { "foo" : "bar" } }
                    }]
                }
            }
            """;

        DummyConfigurationSink.Reset();
        var log = ConfigFromJson(json)
            .CreateLogger();

        log.Write(Some.InformationEvent());

        Assert.NotNull(DummyConfigurationSink.ConfigSection);
        Assert.Equal("bar", DummyConfigurationSink.ConfigSection["foo"]);
    }


    [Fact]
    public void SinkWithConfigurationBindingArgument()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\",
                                   "objectBinding" : [ { "foo" : "bar" }, { "abc" : "xyz" } ] }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void SinkWithStringArrayArgument()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\",
                                   "stringArrayBinding" : [ "foo", "bar", "baz" ] }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Theory]
    [InlineData(".")]
    [InlineData(",")]
    [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/325")]
    public void DestructureNumericNumbers(string numberDecimalSeparator)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;

        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = numberDecimalSeparator;

        Thread.CurrentThread.CurrentCulture = culture;

        try
        {
            // language=json
            var json = """
            {
                "Serilog": {
                    "Using": [ "TestDummies" ],
                    "Destructure": [{
                        "Name": "DummyNumbers",
                        "Args": {
                            "floatValue": 0.1,
                            "doubleValue": 0.2,
                            "decimalValue": 0.3
                        }
                    }]
                }
            }
            """;

            DummyPolicy.Current = null;

            ConfigFromJson(json);

            Assert.NotNull(DummyPolicy.Current);
            Assert.Equal(0.1f, DummyPolicy.Current.Float);
            Assert.Equal(0.2d, DummyPolicy.Current.Double);
            Assert.Equal(0.3m, DummyPolicy.Current.Decimal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void DestructureWithCollectionsOfTypeArgument()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": [ "TestDummies" ],
                    "Destructure": [{
                        "Name": "DummyArrayOfType",
                        "Args": {
                            "list": [
                                "System.Byte",
                                "System.Int16"
                            ],
                            "array" : [
                                "System.Int32",
                                "System.String"
                            ],
                            "type" : "System.TimeSpan",
                            "custom" : [
                                "System.Int64"
                            ],
                            "customString" : [
                                "System.UInt32"
                            ]
                        }
                    }]
                }
            }
            """;

        DummyPolicy.Current = null;

        ConfigFromJson(json);

        Assert.NotNull(DummyPolicy.Current);
        Assert.Equal(typeof(TimeSpan), DummyPolicy.Current.Type);
        Assert.Equal(new[] { typeof(int), typeof(string) }, DummyPolicy.Current.Array);
        Assert.Equal(new[] { typeof(byte), typeof(short) }, DummyPolicy.Current.List!);
        Assert.Equal(typeof(long), DummyPolicy.Current.Custom?.First);
        Assert.Equal("System.UInt32", DummyPolicy.Current.CustomStrings?.First);
    }

    [Fact]
    public void SinkWithIntArrayArgument()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\",
                                   "intArrayBinding" : [ 1,2,3,4,5 ] }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Trait("Bugfix", "#111")]
    [Fact]
    public void CaseInsensitiveArgumentNameMatching()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"PATHFORMAT" : "C:\\"}
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Trait("Bugfix", "#91")]
    [Fact]
    public void WriteToLoggerWithRestrictedToMinimumLevelIsSupported()
    {
        // language=json
        var json = """
        {
            "Serilog": {
                "Using": ["TestDummies"],
                "WriteTo": [{
                    "Name": "Logger",
                    "Args": {
                        "configureLogger" : {
                            "WriteTo": [{
                                "Name": "DummyRollingFile",
                                "Args": {"pathFormat" : "C:\\"}
                            }]},
                        "restrictedToMinimumLevel": "Warning"
                    }
                }]
            }
        }
        """;

        var log = ConfigFromJson(json)
        .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Trait("Bugfix", "#91")]
    [Fact]
    public void WriteToSubLoggerWithLevelSwitchIsSupported()
    {
        // language=json
        var json = """
        {
            "Serilog": {
                "Using": ["TestDummies"],
                "LevelSwitches": {"$switch1" : "Warning" },
                "MinimumLevel" : {
                        "ControlledBy" : "$switch1"
                    },
                "WriteTo": [{
                    "Name": "Logger",
                    "Args": {
                        "configureLogger" : {
                            "WriteTo": [{
                                "Name": "DummyRollingFile",
                                "Args": {"pathFormat" : "C:\\"}
                            }]}
                    }
                }]
            }
        }
        """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();

        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Trait("Bugfix", "#103")]
    [Fact]
    public void InconsistentComplexVsScalarArgumentValuesThrowsIOE()
    {
        // language=json
        var jsonDiscreteValue = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : "C:\\"}
                    }]
                }
            }
            """;

        // language=json
        var jsonComplexValue = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [{
                        "Name": "DummyRollingFile",
                        "Args": {"pathFormat" : { "foo" : "bar" } }
                    }]
                }
            }
            """;

        // These will combine into a ConfigurationSection object that has both
        // Value == "C:\" and GetChildren() == List<string>. No configuration
        // extension matching this exists (in theory an "object" argument could
        // accept either value). ConfigurationReader should throw as soon as
        // the multiple values are recognized; it will never attempt to locate
        // a matching argument.

        var ex = Assert.Throws<InvalidOperationException>(()
            => ConfigFromJson(jsonDiscreteValue, jsonComplexValue));

        Assert.Contains("The value for the argument", ex.Message);
        Assert.Contains("'Serilog:WriteTo:0:Args:pathFormat'", ex.Message);
    }

    [Fact]
    public void DestructureLimitsNestingDepth()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Destructure": [
                    {
                        "Name": "ToMaximumDepth",
                        "Args": { "maximumDestructuringDepth": 3 }
                    }]
                }
            }
            """;

        var NestedObject = new
        {
            A = new
            {
                B = new
                {
                    C = new
                    {
                        D = "F"
                    }
                }
            }
        };

        var msg = GetDestructuredProperty(NestedObject, json);

        Assert.Contains("C", msg);
        Assert.DoesNotContain("D", msg);
    }

    [Fact]
    public void DestructureLimitsStringLength()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Destructure": [
                    {
                        "Name": "ToMaximumStringLength",
                        "Args": { "maximumStringLength": 3 }
                    }]
                }
            }
            """;

        var inputString = "ABCDEFGH";
        var msg = GetDestructuredProperty(inputString, json);

        Assert.Equal("\"AB…\"", msg);
    }

    [Fact]
    public void DestructureLimitsCollectionCount()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Destructure": [
                    {
                        "Name": "ToMaximumCollectionCount",
                        "Args": { "maximumCollectionCount": 3 }
                    }]
                }
            }
            """;

        var collection = new[] { 1, 2, 3, 4, 5, 6 };
        var msg = GetDestructuredProperty(collection, json);

        Assert.Contains("3", msg);
        Assert.DoesNotContain("4", msg);
    }

    private static string GetDestructuredProperty(object x, string json)
    {
        LogEvent? evt = null;
        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();
        log.Information("{@X}", x);
        var result = evt!.Properties["X"].ToString();
        return result;
    }

    [Fact]
    public void DestructuringWithCustomExtensionMethodIsApplied()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "Destructure": [
                    {
                        "Name": "WithDummyHardCodedString",
                        "Args": { "hardCodedString": "hardcoded" }
                    }]
                }
            }
            """;

        LogEvent? evt = null;
        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();
        log.Information("Destructuring with hard-coded policy {@Input}", new { Foo = "Bar" });
        var formattedProperty = evt?.Properties["Input"].ToString();

        Assert.Equal("\"hardcoded\"", formattedProperty);
    }

    [Fact]
    public void DestructuringAsScalarIsAppliedWithShortTypeName()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "Destructure": [
                    {
                        "Name": "AsScalar",
                        "Args": { "scalarType": "System.Version" }
                    }]
                }
            }
            """;

        LogEvent? evt = null;
        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Information("Destructuring as scalar {@Scalarized}", new Version(2, 3));
        var prop = evt?.Properties["Scalarized"];

        Assert.IsType<ScalarValue>(prop);
    }

    [Fact]
    public void DestructuringAsScalarIsAppliedWithAssemblyQualifiedName()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Destructure": [
                    {
                        "Name": "AsScalar",
                        "Args": { "scalarType": "{{typeof(Version).AssemblyQualifiedName}}" }
                    }]
                }
            }
            """;

        LogEvent? evt = null;
        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Information("Destructuring as scalar {@Scalarized}", new Version(2, 3));
        var prop = evt?.Properties["Scalarized"];

        Assert.IsType<ScalarValue>(prop);
    }

    [Fact]
    public void WriteToSinkIsAppliedWithCustomSink()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void WriteToSinkIsAppliedWithCustomSinkAndMinimumLevel()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "WriteTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}",
                            "restrictedToMinimumLevel": "Warning"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void WriteToSinkIsAppliedWithCustomSinkAndLevelSwitch()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "LevelSwitches": {"$switch1": "Warning" },
                    "WriteTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}",
                            "levelSwitch": "$switch1"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void AuditToSinkIsAppliedWithCustomSink()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "AuditTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void AuditToSinkIsAppliedWithCustomSinkAndMinimumLevel()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "AuditTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}",
                            "restrictedToMinimumLevel": "Warning"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void AuditToSinkIsAppliedWithCustomSinkAndLevelSwitch()
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "LevelSwitches": {"$switch1": "Warning" },
                    "AuditTo": [
                    {
                        "Name": "Sink",
                        "Args": {
                            "sink": "{{typeof(DummyRollingFileSink).AssemblyQualifiedName}}",
                            "levelSwitch": "$switch1"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .CreateLogger();

        DummyRollingFileSink.Reset();
        log.Write(Some.InformationEvent());
        log.Write(Some.WarningEvent());

        Assert.Single(DummyRollingFileSink.Emitted);
    }

    [Fact]
    public void EnrichWithIsAppliedWithCustomEnricher()
    {
        LogEvent? evt = null;

        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "Enrich": [
                    {
                        "Name": "With",
                        "Args": {
                            "enricher": "{{typeof(DummyThreadIdEnricher).AssemblyQualifiedName}}"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.Write(Some.InformationEvent());

        Assert.NotNull(evt);
        Assert.True(evt.Properties.ContainsKey("ThreadId"), "Event should have enriched property ThreadId");
    }

    [Fact]
    public void FilterWithIsAppliedWithCustomFilter()
    {
        LogEvent? evt = null;

        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "Using": ["TestDummies"],
                    "Filter": [
                    {
                        "Name": "With",
                        "Args": {
                            "filter": "{{typeof(DummyAnonymousUserFilter).AssemblyQualifiedName}}"
                        }
                    }]
                }
            }
            """;

        var log = ConfigFromJson(json)
            .WriteTo.Sink(new DelegatingSink(e => evt = e))
            .CreateLogger();

        log.ForContext("User", "anonymous").Write(Some.InformationEvent());
        Assert.Null(evt);
        log.ForContext("User", "the user").Write(Some.InformationEvent());
        Assert.NotNull(evt);
    }

    [Theory]
    [InlineData("$switch1")]
    [InlineData("switch1")]
    public void TestLogLevelSwitchesCallback(string switchName)
    {
        // language=json
        var json = $$"""
            {
                "Serilog": {
                    "LevelSwitches": { "{{switchName}}": "Information" },
                    "MinimumLevel": {
                        "Override": {
                            "System": "Warning",
                            "System.Threading": "Debug"
                        }
                    }
                }
            }
            """;

        IDictionary<string, LoggingLevelSwitch> switches = new Dictionary<string, LoggingLevelSwitch>();
        var readerOptions = new ConfigurationReaderOptions { OnLevelSwitchCreated = (name, levelSwitch) => switches[name] = levelSwitch };
        ConfigFromJson(json, options: readerOptions);

        Assert.Equal(3, switches.Count);

        var switch1 = Assert.Contains("$switch1", switches);
        Assert.Equal(LogEventLevel.Information, switch1.MinimumLevel);

        var system = Assert.Contains("System", switches);
        Assert.Equal(LogEventLevel.Warning, system.MinimumLevel);

        var systemThreading = Assert.Contains("System.Threading", switches);
        Assert.Equal(LogEventLevel.Debug, systemThreading.MinimumLevel);
    }

    [Fact]
    public void TestLogFilterSwitchesCallback()
    {
        // language=json
        var json = """
            {
                "Serilog": {
                    "FilterSwitches": {
                        "switch1": "Prop = 1",
                        "$switch2": "Prop = 2"
                    }
                }
            }
            """;

        IDictionary<string, ILoggingFilterSwitch> switches = new Dictionary<string, ILoggingFilterSwitch>();
        var readerOptions = new ConfigurationReaderOptions { OnFilterSwitchCreated = (name, filterSwitch) => switches[name] = filterSwitch };
        ConfigFromJson(json, options: readerOptions);

        Assert.Equal(2, switches.Count);

        var switch1 = Assert.Contains("switch1", switches);
        Assert.Equal("Prop = 1", switch1.Expression);

        var switch2 = Assert.Contains("$switch2", switches);
        Assert.Equal("Prop = 2", switch2.Expression);
    }
}
