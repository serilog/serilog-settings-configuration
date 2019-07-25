using System;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;
using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class LoggerConfigurationExtensionsTests
    {
        [Fact]
        public void ReadFromConfigurationShouldNotThrowOnEmptyConfiguration()
        {
            Action act = () => new LoggerConfiguration().ReadFrom.Configuration(new ConfigurationBuilder().Build());

            // should not throw
            act();
        }

        [Fact]
        [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/143")]
        public void ReadFromConfigurationSectionReadsFromAnArbitrarySection()
        {
            LogEvent evt = null;

            var json = @"{
		        ""NotSerilog"": {            
			        ""Properties"": {
				        ""App"": ""Test""
			        }
		        }
	        }";

            var config = new ConfigurationBuilder()
                .AddJsonString(json)
                .Build();

#pragma warning disable CS0618 // Type or member is obsolete
            var log = new LoggerConfiguration()
                .ReadFrom.ConfigurationSection(config.GetSection("NotSerilog"))
#pragma warning restore CS0618 // Type or member is obsolete
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Information("Has a test property");

            Assert.NotNull(evt);
            Assert.Equal("Test", evt.Properties["App"].LiteralValue());
        }

        [Fact]
        [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/143")]
        public void ReadFromConfigurationSectionThrowsWhenTryingToCallConfigurationMethodWithIConfigurationParam()
        {
            var json = @"{
                ""NotSerilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithConfiguration"",
                        ""Args"": {}
                    }]        
                }
            }";

            var config = new ConfigurationBuilder()
                .AddJsonString(json)
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
#pragma warning disable CS0618 // Type or member is obsolete
               new LoggerConfiguration()
                   .ReadFrom.ConfigurationSection(config.GetSection("NotSerilog"))
#pragma warning restore CS0618 // Type or member is obsolete
                   .CreateLogger());

            Assert.Equal("Trying to invoke a configuration method accepting a `IConfiguration` argument. " +
                         "This is not supported when only a `IConfigSection` has been provided. " +
                         "(method 'Serilog.LoggerConfiguration DummyWithConfiguration(Serilog.Configuration.LoggerSinkConfiguration, Microsoft.Extensions.Configuration.IConfiguration, Serilog.Events.LogEventLevel)')",
                exception.Message);

        }

        [Fact]
        public void ReadFromConfigurationDoesNotThrowWhenTryingToCallConfigurationMethodWithIConfigurationParam()
        {
            var json = @"{
                ""NotSerilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithConfiguration"",
                        ""Args"": {}
                    }]        
                }
            }";

            var config = new ConfigurationBuilder()
                .AddJsonString(json)
                .Build();

            _ = new LoggerConfiguration()
                   .ReadFrom.Configuration(config, "NotSerilog")
                   .CreateLogger();

        }

        [Fact]
        [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/143")]
        public void ReadFromConfigurationSectionDoesNotThrowWhenTryingToCallConfigurationMethodWithOptionalIConfigurationParam()
        {
            var json = @"{
                ""NotSerilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithOptionalConfiguration"",
                        ""Args"": {}
                    }]        
                }
            }";

            var config = new ConfigurationBuilder()
                .AddJsonString(json)
                .Build();

            // this should not throw because DummyWithOptionalConfiguration accepts an optional config
#pragma warning disable CS0618 // Type or member is obsolete
            new LoggerConfiguration()
                .ReadFrom.ConfigurationSection(config.GetSection("NotSerilog"))
#pragma warning restore CS0618 // Type or member is obsolete
                .CreateLogger();

        }
    }
}
