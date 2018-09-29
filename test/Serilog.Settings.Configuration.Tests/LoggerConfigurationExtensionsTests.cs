using System;
using Microsoft.Extensions.Configuration;
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
    }
}
