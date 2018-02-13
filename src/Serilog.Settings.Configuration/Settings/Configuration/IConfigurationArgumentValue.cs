using System;
using System.Collections.Generic;
using Serilog.Core;

namespace Serilog.Settings.Configuration
{
    interface IConfigurationArgumentValue
    {
        object ConvertTo(Type toType, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches);
    }
}
