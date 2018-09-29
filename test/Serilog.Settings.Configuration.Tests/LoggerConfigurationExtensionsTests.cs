using System;
using Microsoft.Extensions.Configuration;
using Serilog.Settings.Configuration.Tests.Support;
using TestDummies.Console;
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
                    dependencyContext: null,
                    onNullDependencyContext: ConfigurationAssemblySource.AlwaysScanDllFiles)
                .CreateLogger();

            DummyConsoleSink.Emitted.Clear();

            log.Write(Some.InformationEvent());

            Assert.Equal(1, DummyConsoleSink.Emitted.Count);
        }
    }
}
