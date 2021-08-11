using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration.Tests.Support;

using Xunit;
using Microsoft.Extensions.Configuration;

using TestDummies.Console;

namespace Serilog.Settings.Configuration.Tests
{
    public class DynamicLevelChangeTests
    {
        const string DefaultConfig = @"{
            'Serilog': {
                'Using': [ 'TestDummies' ],
                'MinimumLevel': {
                    'Default': 'Information',
                    'Override': {
                        'Root.Test': 'Information'
                    }
                },
                'LevelSwitches': { '$mySwitch': 'Information' },
                'FilterSwitches': { '$myFilter': null },
                'Filter:Dummy': {
                    'Name': 'ControlledBy',
                    'Args': {
                        'switch': '$myFilter'
                    }
                },
                'WriteTo:Dummy': {
                    'Name': 'DummyConsole',
                    'Args': {
                        'levelSwitch': '$mySwitch'
                    }
                }
            }
        }";

        readonly ReloadableConfigurationSource _configSource;

        public DynamicLevelChangeTests()
        {
            _configSource = new ReloadableConfigurationSource(JsonStringConfigSource.LoadData(DefaultConfig));
        }

        [Fact]
        public void ShouldRespectDynamicLevelChanges()
        {
            using (var logger = new LoggerConfiguration()
                .ReadFrom
                .Configuration(new ConfigurationBuilder().Add(_configSource).Build())
                .CreateLogger())
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

                DummyConsoleSink.Emitted.Clear();
                UpdateConfig(filterExpression: "Prop = 'Val_1'");
                logger.Write(Some.DebugEvent());
                logger.ForContext("Prop", "Val_1").Write(Some.DebugEvent());
                Assert.Single(DummyConsoleSink.Emitted);

                DummyConsoleSink.Emitted.Clear();
                UpdateConfig(filterExpression: "Prop = 'Val_2'");
                logger.Write(Some.DebugEvent());
                logger.ForContext("Prop", "Val_1").Write(Some.DebugEvent());
                Assert.Empty(DummyConsoleSink.Emitted);
            }
        }

        void UpdateConfig(LogEventLevel? minimumLevel = null, LogEventLevel? switchLevel = null, LogEventLevel? overrideLevel = null, string filterExpression = null)
        {
            if (minimumLevel.HasValue)
            {
                _configSource.Set("Serilog:MinimumLevel:Default", minimumLevel.Value.ToString());
            }

            if (switchLevel.HasValue)
            {
                _configSource.Set("Serilog:LevelSwitches:$mySwitch", switchLevel.Value.ToString());
            }

            if (overrideLevel.HasValue)
            {
                _configSource.Set("Serilog:MinimumLevel:Override:Root.Test", overrideLevel.Value.ToString());
            }

            if (filterExpression != null)
            {
                _configSource.Set("Serilog:FilterSwitches:$myFilter", filterExpression);
            }

            _configSource.Reload();
        }
    }
}
