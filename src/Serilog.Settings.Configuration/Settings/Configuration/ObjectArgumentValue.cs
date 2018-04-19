using Microsoft.Extensions.Configuration;
using Serilog.Core;
using System;
using System.Collections.Generic;

namespace Serilog.Settings.Configuration
{
    class ObjectArgumentValue : IConfigurationArgumentValue
    {
        readonly IConfigurationSection section;

        public ObjectArgumentValue(IConfigurationSection section)
        {
            this.section = section;
        }

        public object ConvertTo(Type toType, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            if(toType == typeof(IConfigurationSection)) return section;
            return section.Get(toType);
        }
    }
}
