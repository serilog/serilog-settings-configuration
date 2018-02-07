using System;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;
using TestDummies;
using TestDummies.Console;
using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationSettingsTests
    {
        private static LoggerConfiguration ConfigFromJson(string jsonString)
        {
            var config = new ConfigurationBuilder().AddJsonString(jsonString).Build();
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config);
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

            DummyRollingFileSink.Emitted.Clear();
            DummyRollingFileAuditSink.Emitted.Clear();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyRollingFileSink.Emitted.Count);
            Assert.Equal(0, DummyRollingFileAuditSink.Emitted.Count);
        }

        //[Fact]
        //public void AuditSinksAreConfigured()
        //{
        //    var settings = new Dictionary<string, string>
        //    {
        //        ["using:TestDummies"] = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().Assembly.FullName,
        //        ["audit-to:DummyRollingFile.pathFormat"] = "C:\\"
        //    };

        //    var log = new LoggerConfiguration()
        //        .ReadFrom.KeyValuePairs(settings)
        //        .CreateLogger();

        //    DummyRollingFileSink.Emitted.Clear();
        //    DummyRollingFileAuditSink.Emitted.Clear();

        //    log.Write(Some.InformationEvent());

        //    Assert.Equal(0, DummyRollingFileSink.Emitted.Count);
        //    Assert.Equal(1, DummyRollingFileAuditSink.Emitted.Count);
        //}

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

        //[Fact]
        //public void SinksAreConfiguredWithStaticMember()
        //{
        //    var json = @"{
        //        ""Serilog"": {            
        //            ""Using"": [""TestDummies""],
        //            ""WriteTo"": [{
        //                ""Name"": ""DummyConsole"",
        //                ""Args"": {""theme"" : ""TestDummies.Console.Themes.ConsoleThemes::Theme1, TestDummies""}
        //            }]        
        //        }
        //    }";

        //    DummyConsoleSink.Theme = null;

        //    ConfigFromJson(json)
        //        .CreateLogger();

        //    Assert.Equal(ConsoleThemes.Theme1, DummyConsoleSink.Theme);
        //}
    }
}
