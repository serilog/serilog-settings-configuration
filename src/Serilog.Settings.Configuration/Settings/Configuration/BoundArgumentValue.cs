using Microsoft.Extensions.Configuration;
using Serilog.Core;
using System;
using System.Collections.Generic;

namespace Serilog.Settings.Configuration
{
    class BoundArgumentValue : IConfigurationArgumentValue
    {
        readonly IConfigurationSection section;

        public BoundArgumentValue(IConfigurationSection section)
        {
            this.section = section;
        }

        public object ConvertTo(Type toType, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            return section.Get(toType);
        }
    }
}
