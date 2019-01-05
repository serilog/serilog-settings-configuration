using System;
using System.IO;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;

using Xunit;
using TestDummies.Console;

namespace Serilog.Settings.Configuration.Tests
{
    public class DynamicLevelChangeTests : IDisposable
    {
        const string ConfigFilename = "dynamicLevels.json";

        readonly IConfigurationRoot _config;

        LogEventLevel _minimumLevel, _overrideLevel, _switchLevel;

        public DynamicLevelChangeTests()
        {
            UpdateConfig(LogEventLevel.Information, LogEventLevel.Information, LogEventLevel.Information);

            _config = new ConfigurationBuilder()
                .AddJsonFile(ConfigFilename, false, true)
                .Build();
        }

        public void Dispose()
        {
            if (File.Exists(ConfigFilename))
            {
                File.Delete(ConfigFilename);
            }
        }

        [Fact]
        public void ShouldRespectDynamicLevelChanges()
        {
            using (var logger = new LoggerConfiguration().ReadFrom.Configuration(_config).CreateLogger())
            {
                DummyConsoleSink.Emitted.Clear();
                logger.Write(Some.DebugEvent());
                Assert.Empty(DummyConsoleSink.Emitted);

                DummyConsoleSink.Emitted.Clear();
                UpdateConfig(minimumLevel: LogEventLevel.Debug);
                logger.Write(Some.DebugEvent());
                Assert.Empty(DummyConsoleSink.Emitted);

                DummyConsoleSink.Emitted.Clear();
                UpdateConfig(switchLevel: LogEventLevel.Debug);
                logger.Write(Some.DebugEvent());
                logger.ForContext(Constants.SourceContextPropertyName, "Root.Test").Write(Some.DebugEvent());
                Assert.Single(DummyConsoleSink.Emitted);

                DummyConsoleSink.Emitted.Clear();
                UpdateConfig(overrideLevel: LogEventLevel.Debug);
                logger.ForContext(Constants.SourceContextPropertyName, "Root.Test").Write(Some.DebugEvent());
                Assert.Single(DummyConsoleSink.Emitted);
            }
        }

        void UpdateConfig(LogEventLevel? minimumLevel = null, LogEventLevel? overrideLevel = null, LogEventLevel? switchLevel = null)
        {
            File.WriteAllText(ConfigFilename, BuildConfiguration());
            Thread.Sleep(300);

            string BuildConfiguration()
            {
                _minimumLevel = minimumLevel ?? _minimumLevel;
                _overrideLevel = overrideLevel ?? _overrideLevel;
                _switchLevel = switchLevel ?? _switchLevel;

                var config = @"{
                    'Serilog': {
                        'Using': [ 'TestDummies' ],
                        'MinimumLevel': {
                            'Default': '" + _minimumLevel + @"',
                            'Override': {
                                'Root.Test': '" + _overrideLevel + @"'
                            }
                        },
                        'LevelSwitches': { '$mySwitch': '" + _switchLevel + @"' },
                        'WriteTo:Dummy': {
                            'Name': 'DummyConsole',
                            'Args': {
                                'levelSwitch': '$mySwitch'
                            }
                        }
                    }
                }";

                return config;
            }
        }
    }
}
