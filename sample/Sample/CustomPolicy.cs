using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace Sample;

public class CustomPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;

        if (value is LoginData loginData)
        {
            result = new StructureValue(
                new List<LogEventProperty>
                {
                    new("Username", new ScalarValue(loginData.Username))
                });
        }

        return (result != null);
    }
}
