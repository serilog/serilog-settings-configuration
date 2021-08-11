using System;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;
using TestDummies;
using TestDummies.Console;
using TestDummies.Console.Themes;
using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationSettingsTests
    {
        static LoggerConfiguration ConfigFromJson(string jsonString, string secondJsonSource = null)
        {
            return ConfigFromJson(jsonString, secondJsonSource, out _);
        }

        static LoggerConfiguration ConfigFromJson(string jsonString, out IConfiguration configuration)
        {
            return ConfigFromJson(jsonString, null, out configuration);
        }

        static LoggerConfiguration ConfigFromJson(string jsonString, string secondJsonSource, out IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder().AddJsonString(jsonString);
            if (secondJsonSource != null)
                builder.AddJsonString(secondJsonSource);
            configuration = builder.Build();
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);
        }

        [Fact]
        public void PropertyEnrichmentIsApplied()
        {
            LogEvent evt = null;

            var json = @"{
                ""Serilog"": {            
                    ""Properties"": {
                        ""App"": ""Test""
                    }
                }
            }";

            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Information("Has a test property");

            Assert.NotNull(evt);
            Assert.Equal("Test", evt.Properties["App"].LiteralValue());
        }

        [Theory]
        [InlineData("extended syntax",
            @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [
                        { ""Name"": ""DummyConsole""},
                        { ""Name"": ""DummyWithLevelSwitch""},
                    ]        
                }
            }")]
        [InlineData("simplified syntax",
            @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [""DummyConsole"", ""DummyWithLevelSwitch"" ]        
                }
            }")]
        public void ParameterlessSinksAreConfigured(string syntax, string json)
        {
            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyConsoleSink.Emitted.Clear();
            DummyWithLevelSwitchSink.Emitted.Clear();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyConsoleSink.Emitted.Count);
            Assert.Equal(1, DummyWithLevelSwitchSink.Emitted.Count);
        }

        [Fact]
        public void ConfigurationAssembliesFromDllScanning()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [""DummyConsole""]
                }
            }";

            var builder = new ConfigurationBuilder().AddJsonString(json);
            var config = builder.Build();
            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(
                    configuration: config,
                    configurationAssemblySource: ConfigurationAssemblySource.AlwaysScanDllFiles)
                .CreateLogger();

            DummyConsoleSink.Emitted.Clear();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyConsoleSink.Emitted.Count);
        }

        [Fact]
        public void SinksAreConfigured()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\""}
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();
            DummyRollingFileAuditSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
            Assert.Equal(0, DummyRollingFileAuditSink.Emitted.Count);
        }

        [Fact]
        public void AuditSinksAreConfigured()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""AuditTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\""}
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();
            DummyRollingFileAuditSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(0, DummyRollingFileSink.Emitted.Count);
            Assert.Equal(1, DummyRollingFileAuditSink.Emitted.Count);
        }

        [Fact]
        public void AuditToSubLoggersAreConfigured()
        {
            var json = @"{
            ""Serilog"": {            
                ""Using"": [""TestDummies""],       
                ""AuditTo"": [{
                    ""Name"": ""Logger"",
                    ""Args"": {
                        ""configureLogger"" : {
                            ""AuditTo"": [{
                                ""Name"": ""DummyRollingFile"",
                                ""Args"": {""pathFormat"" : ""C:\\""}
                            }]}
                    }
                }]        
            }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();
            DummyRollingFileAuditSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(0, DummyRollingFileSink.Emitted.Count);
            Assert.Equal(1, DummyRollingFileAuditSink.Emitted.Count);
        }

        [Fact]
        public void TestMinimumLevelOverrides()
        {
            var json = @"{
                ""Serilog"": {
                    ""MinimumLevel"" : {
                        ""Override"" : {
                            ""System"" : ""Warning""
                        }
                    }        
                }
            }";

            LogEvent evt = null;

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
            var json = @"{
                ""Serilog"": {
                    ""MinimumLevel"" : {
                        ""Default"" : ""Warning"",
                        ""Override"" : {
                            ""System"" : ""Warning"",
                            ""System.Threading"": ""Debug""
                        }
                    }        
                }
            }";

            LogEvent evt = null;

            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Write(Some.DebugEvent());
            Assert.Null(evt);

            var custom = log.ForContext(Constants.SourceContextPropertyName, typeof(System.Threading.Tasks.Task).FullName + "<42>");
            custom.Write(Some.DebugEvent());
            Assert.NotNull(evt);
            
            evt = null;
            var systemThreadingLogger = log.ForContext<System.Threading.Tasks.Task>();
            systemThreadingLogger.Write(Some.DebugEvent());
            Assert.NotNull(evt);              
        }

        [Fact]
        public void SinksWithAbstractParamsAreConfiguredWithTypeName()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyConsole"",
                        ""Args"": {""theme"" : ""Serilog.Settings.Configuration.Tests.Support.CustomConsoleTheme, Serilog.Settings.Configuration.Tests""}
                    }]        
                }
            }";

            DummyConsoleSink.Theme = null;

            ConfigFromJson(json)
                .CreateLogger();

            Assert.NotNull(DummyConsoleSink.Theme);
            Assert.IsType<CustomConsoleTheme>(DummyConsoleSink.Theme);
        }

        [Fact]
        public void SinksAreConfiguredWithStaticMember()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyConsole"",
                        ""Args"": {""theme"" : ""TestDummies.Console.Themes.ConsoleThemes::Theme1, TestDummies""}
                    }]        
                }
            }";

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
            var json = @"{
                ""Serilog"": {
                    ""LevelSwitches"": {""1InvalidSwitchName"" : ""Warning"" }
                }
            }";

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
            var json = $@"{{
                'Serilog': {{
                    'FilterSwitches': {{ '{switchName}': 'Prop = 42' }},
                    'Filter:BySwitch': {{
                        'Name': 'ControlledBy',
                        'Args': {{
                            'switch': '$mySwitch'
                        }}
                    }}
                }}
            }}";
            LogEvent evt = null;

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
            var json = $@"{{
                'Serilog': {{
                    'LevelSwitches': {{ '{switchName}' : 'Warning' }},
                    'MinimumLevel' : {{
                        'ControlledBy' : '$switch1'
                    }}
                }}
            }}";
            LogEvent evt = null;

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
            var json = @"{
                ""Serilog"": {            
                    ""LevelSwitches"": {""$switch1"" : ""Warning"" },
                    ""MinimumLevel"" : {
                        ""ControlledBy"" : ""$switch2""
                    }
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ConfigFromJson(json)
                    .CreateLogger());

            Assert.Contains("$switch2", ex.Message);
            Assert.Contains("\"LevelSwitches\":{\"$switch2\":", ex.Message);
        }

        [Fact]
        public void LoggingLevelSwitchIsPassedToSinks()
        {
            var json = @"{
                ""Serilog"": {      
                    ""Using"": [""TestDummies""],
                    ""LevelSwitches"": {""$switch1"" : ""Information"" },
                    ""MinimumLevel"" : {
                        ""ControlledBy"" : ""$switch1""
                    },
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithLevelSwitch"",
                        ""Args"": {""controlLevelSwitch"" : ""$switch1""}
                    }]      
                }
            }";

            LogEvent evt = null;

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
            var json = @"{
                ""Serilog"": {      
                    ""Using"": [""TestDummies""],
                    ""LevelSwitches"": {""$switch1"" : ""Information"" },
                    ""MinimumLevel"" : {
                        ""ControlledBy"" : ""$switch1""
                    },
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithLevelSwitch"",
                        ""Args"": {""controlLevelSwitch"" : ""$switch2""}
                    }]      
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ConfigFromJson(json)
                    .CreateLogger());

            Assert.Contains("$switch2", ex.Message);
            Assert.Contains("\"LevelSwitches\":{\"$switch2\":", ex.Message);
        }

        [Fact]
        public void LoggingLevelSwitchCanBeUsedForMinimumLevelOverrides()
        {
            var json = @"{
                ""Serilog"": {
                    ""Using"": [""TestDummies""],
                    ""LevelSwitches"": {""$specificSwitch"" : ""Warning"" },
                    ""MinimumLevel"" : {
                        ""Default"" : ""Debug"",
                        ""Override"" : {
                            ""System"" : ""$specificSwitch""
                        }
                    },
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithLevelSwitch"",
                        ""Args"": {""controlLevelSwitch"" : ""$specificSwitch""}
                    }]     
                }
            }";

            LogEvent evt = null;

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

            controlSwitch.MinimumLevel = LogEventLevel.Information;
            systemLogger.Write(Some.InformationEvent());
            Assert.False(evt is null, "LoggingLevelSwitch level was changed to Information for logger System.*. It should now log Information events for SourceContext System.Bar.");
            // ReSharper restore HeuristicUnreachableCode
        }

        [Fact]

        [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/142")]
        public void SinkWithIConfigurationArguments()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithConfiguration"",
                        ""Args"": {}
                    }]        
                }
            }";

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
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithOptionalConfiguration"",
                        ""Args"": {}
                    }]        
                }
            }";

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
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithConfigSection"",
                        ""Args"": {""configurationSection"" : { ""foo"" : ""bar"" } }
                    }]        
                }
            }";

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
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\"",
                                   ""objectBinding"" : [ { ""foo"" : ""bar"" }, { ""abc"" : ""xyz"" } ] }
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Fact]
        public void SinkWithStringArrayArgument()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\"",
                                   ""stringArrayBinding"" : [ ""foo"", ""bar"", ""baz"" ] }
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Fact]
        public void DestructureWithCollectionsOfTypeArgument()
        {
            var json = @"{
                ""Serilog"": {
                    ""Using"": [ ""TestDummies"" ],
                    ""Destructure"": [{
                        ""Name"": ""DummyArrayOfType"",
                        ""Args"": {
                            ""list"": [
                                ""System.Byte"",
                                ""System.Int16""
                            ],
                            ""array"" : [
                                ""System.Int32"",
                                ""System.String""
                            ],
                            ""type"" : ""System.TimeSpan"",
                            ""custom"" : [
                                ""System.Int64""
                            ],
                            ""customString"" : [
                                ""System.UInt32""
                            ]
                        }
                    }]        
                }
            }";

            DummyPolicy.Current = null;

            ConfigFromJson(json);

            Assert.Equal(typeof(TimeSpan), DummyPolicy.Current.Type);
            Assert.Equal(new[] { typeof(int), typeof(string) }, DummyPolicy.Current.Array);
            Assert.Equal(new[] { typeof(byte), typeof(short) }, DummyPolicy.Current.List);
            Assert.Equal(typeof(long), DummyPolicy.Current.Custom.First);
            Assert.Equal("System.UInt32", DummyPolicy.Current.CustomStrings.First);
        }

        [Fact]
        public void SinkWithIntArrayArgument()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\"",
                                   ""intArrayBinding"" : [ 1,2,3,4,5 ] }
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Trait("Bugfix", "#111")]
        [Fact]
        public void CaseInsensitiveArgumentNameMatching()
        {
            var json = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""PATHFORMAT"" : ""C:\\""}
                    }]        
                }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Trait("Bugfix", "#91")]
        [Fact]
        public void WriteToLoggerWithRestrictedToMinimumLevelIsSupported()
        {
            var json = @"{
            ""Serilog"": {            
                ""Using"": [""TestDummies""],
                ""WriteTo"": [{
                    ""Name"": ""Logger"",
                    ""Args"": {
                        ""configureLogger"" : {
                            ""WriteTo"": [{
                                ""Name"": ""DummyRollingFile"",
                                ""Args"": {""pathFormat"" : ""C:\\""}
                            }]},
                        ""restrictedToMinimumLevel"": ""Warning"" 
                    }
                }]        
            }
            }";

            var log = ConfigFromJson(json)
            .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());
            log.Write(Some.WarningEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Trait("Bugfix", "#91")]
        [Fact]
        public void WriteToSubLoggerWithLevelSwitchIsSupported()
        {
            var json = @"{
            ""Serilog"": {            
                ""Using"": [""TestDummies""],
                ""LevelSwitches"": {""$switch1"" : ""Warning"" },          
                ""MinimumLevel"" : {
                        ""ControlledBy"" : ""$switch1""
                    },
                ""WriteTo"": [{
                    ""Name"": ""Logger"",
                    ""Args"": {
                        ""configureLogger"" : {
                            ""WriteTo"": [{
                                ""Name"": ""DummyRollingFile"",
                                ""Args"": {""pathFormat"" : ""C:\\""}
                            }]}
                    }
                }]        
            }
            }";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();

            log.Write(Some.InformationEvent());
            log.Write(Some.WarningEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
        }

        [Trait("Bugfix", "#103")]
        [Fact]
        public void InconsistentComplexVsScalarArgumentValuesThrowsIOE()
        {
            var jsonDiscreteValue = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : ""C:\\""}
                    }]        
                }
            }";

            var jsonComplexValue = @"{
                ""Serilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyRollingFile"",
                        ""Args"": {""pathFormat"" : { ""foo"" : ""bar"" } }
                    }]        
                }
            }";

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
            var json = @"{
                ""Serilog"": {
                    ""Destructure"": [
                    {
                        ""Name"": ""ToMaximumDepth"",
                        ""Args"": { ""maximumDestructuringDepth"": 3 }
                    }]
                }
            }";

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
            var json = @"{
                ""Serilog"": {
                    ""Destructure"": [
                    {
                        ""Name"": ""ToMaximumStringLength"",
                        ""Args"": { ""maximumStringLength"": 3 }
                    }]
                }
            }";

            var inputString = "ABCDEFGH";
            var msg = GetDestructuredProperty(inputString, json);

            Assert.Equal("\"AB…\"", msg);
        }

        [Fact]
        public void DestructureLimitsCollectionCount()
        {
            var json = @"{
                ""Serilog"": {
                    ""Destructure"": [
                    {
                        ""Name"": ""ToMaximumCollectionCount"",
                        ""Args"": { ""maximumCollectionCount"": 3 }
                    }]
                }
            }";

            var collection = new[] { 1, 2, 3, 4, 5, 6 };
            var msg = GetDestructuredProperty(collection, json);

            Assert.Contains("3", msg);
            Assert.DoesNotContain("4", msg);
        }

        private static string GetDestructuredProperty(object x, string json)
        {
            LogEvent evt = null;
            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();
            log.Information("{@X}", x);
            var result = evt.Properties["X"].ToString();
            return result;
        }

        [Fact]
        public void DestructuringWithCustomExtensionMethodIsApplied()
        {
            var json = @"{
                ""Serilog"": {
                    ""Using"": [""TestDummies""],
                    ""Destructure"": [
                    {
                        ""Name"": ""WithDummyHardCodedString"",
                        ""Args"": { ""hardCodedString"": ""hardcoded"" }
                    }]
                }
            }";

            LogEvent evt = null;
            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();
            log.Information("Destructuring with hard-coded policy {@Input}", new { Foo = "Bar" });
            var formattedProperty = evt.Properties["Input"].ToString();

            Assert.Equal("\"hardcoded\"", formattedProperty);
        }

        [Fact]
        public void DestructuringAsScalarIsAppliedWithShortTypeName()
        {
            var json = @"{
                ""Serilog"": {
                    ""Destructure"": [
                    {
                        ""Name"": ""AsScalar"",
                        ""Args"": { ""scalarType"": ""System.Version"" }
                    }]
                }
            }";

            LogEvent evt = null;
            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Information("Destructuring as scalar {@Scalarized}", new Version(2, 3));
            var prop = evt.Properties["Scalarized"];

            Assert.IsType<ScalarValue>(prop);
        }

        [Fact]
        public void DestructuringAsScalarIsAppliedWithAssemblyQualifiedName()
        {
            var json = $@"{{
                ""Serilog"": {{
                    ""Destructure"": [
                    {{
                        ""Name"": ""AsScalar"",
                        ""Args"": {{ ""scalarType"": ""{typeof(Version).AssemblyQualifiedName}"" }}
                    }}]
                }}
            }}";

            LogEvent evt = null;
            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Information("Destructuring as scalar {@Scalarized}", new Version(2, 3));
            var prop = evt.Properties["Scalarized"];

            Assert.IsType<ScalarValue>(prop);
        }

        [Fact]
        public void WriteToSinkIsAppliedWithCustomSink()
        {
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}""
                        }}
                    }}]
                }}
            }}";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();
            log.Write(Some.InformationEvent());

            Assert.Single(DummyRollingFileSink.Emitted);
        }

        [Fact]
        public void WriteToSinkIsAppliedWithCustomSinkAndMinimumLevel()
        {
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}"",
                            ""restrictedToMinimumLevel"": ""Warning""
                        }}
                    }}]
                }}
            }}";

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
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""LevelSwitches"": {{""$switch1"": ""Warning"" }},
                    ""WriteTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}"",
                            ""levelSwitch"": ""$switch1""
                        }}
                    }}]
                }}
            }}";

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
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""AuditTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}""
                        }}
                    }}]
                }}
            }}";

            var log = ConfigFromJson(json)
                .CreateLogger();

            DummyRollingFileSink.Reset();
            log.Write(Some.InformationEvent());

            Assert.Single(DummyRollingFileSink.Emitted);
        }

        [Fact]
        public void AuditToSinkIsAppliedWithCustomSinkAndMinimumLevel()
        {
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""AuditTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}"",
                            ""restrictedToMinimumLevel"": ""Warning""
                        }}
                    }}]
                }}
            }}";

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
            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""LevelSwitches"": {{""$switch1"": ""Warning"" }},
                    ""AuditTo"": [
                    {{
                        ""Name"": ""Sink"",
                        ""Args"": {{
                            ""sink"": ""{typeof(DummyRollingFileSink).AssemblyQualifiedName}"",
                            ""levelSwitch"": ""$switch1""
                        }}
                    }}]
                }}
            }}";

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
            LogEvent evt = null;

            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""Enrich"": [
                    {{
                        ""Name"": ""With"",
                        ""Args"": {{
                            ""enricher"": ""{typeof(DummyThreadIdEnricher).AssemblyQualifiedName}""
                        }}
                    }}]
                }}
            }}";

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
            LogEvent evt = null;

            var json = $@"{{
                ""Serilog"": {{
                    ""Using"": [""TestDummies""],
                    ""Filter"": [
                    {{
                        ""Name"": ""With"",
                        ""Args"": {{
                            ""filter"": ""{typeof(DummyAnonymousUserFilter).AssemblyQualifiedName}""
                        }}
                    }}]
                }}
            }}";

            var log = ConfigFromJson(json)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.ForContext("User", "anonymous").Write(Some.InformationEvent());
            Assert.Null(evt);
            log.ForContext("User", "the user").Write(Some.InformationEvent());
            Assert.NotNull(evt);
        }
    }
}
