using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Core;

namespace Serilog.Settings.Configuration
{
    interface IConfigurationReader : ILoggerSettings
    {
        void ApplySinks(LoggerSinkConfiguration loggerSinkConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches);
    }
}
