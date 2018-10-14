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

            var log = new LoggerConfiguration()
                .ReadFrom.ConfigurationSection(config.GetSection("NotSerilog"))
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            log.Information("Has a test property");

            Assert.NotNull(evt);
            Assert.Equal("Test", evt.Properties["App"].LiteralValue());
        }

        [Fact(Skip = "Passes when run alone, but fails when the whole suite is run - to fix")]
        [Trait("BugFix", "https://github.com/serilog/serilog-settings-configuration/issues/143")]
        public void ReadFromConfigurationSectionThrowsWhenTryingToCallConfigurationMethodWithIConfigurationParam()
        {
            var json = @"{
                ""NotSerilog"": {            
                    ""Using"": [""TestDummies""],
                    ""WriteTo"": [{
                        ""Name"": ""DummyWithConfiguration"",
                        ""Args"": {""pathFormat"" : ""C:\\"",
                                   ""configurationSection"" : { ""foo"" : ""bar"" } }
                    }]        
                }
            }";

            var config = new ConfigurationBuilder()
                .AddJsonString(json)
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() =>
               new LoggerConfiguration()
                   .ReadFrom.ConfigurationSection(config.GetSection("NotSerilog"))
                   .CreateLogger());

            Assert.Equal("Trying to invoke a configuration method accepting a `IConfiguration` argument. " +
                         "This is not supported when only a `IConfigSection` has been provided. " +
                         "(method 'Serilog.LoggerConfiguration DummyWithConfiguration(Serilog.Configuration.LoggerSinkConfiguration, Microsoft.Extensions.Configuration.IConfiguration, Microsoft.Extensions.Configuration.IConfigurationSection, System.String, Serilog.Events.LogEventLevel)')",
                exception.Message);

        }
    }
}
