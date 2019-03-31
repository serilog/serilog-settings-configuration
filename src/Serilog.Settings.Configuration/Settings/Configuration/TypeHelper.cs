using System;
using System.Linq;

namespace Serilog.Settings.Configuration
{
    internal static class TypeHelper
    {
        internal static Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                if (!typeName.Contains(','))
                {
                    type = Type.GetType($"{typeName}, Serilog");
                }
            }

            return type;
        }
    }
}
