using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Serilog.Settings.Configuration;

abstract class ConfigurationArgumentValue
{
    public abstract object? ConvertTo(Type toType, ResolutionContext resolutionContext);

    public static ConfigurationArgumentValue FromSection(IConfigurationSection argumentSection, IReadOnlyCollection<Assembly> configurationAssemblies)
    {
        ConfigurationArgumentValue argumentValue;

        // Reject configurations where an element has both scalar and complex
        // values as a result of reading multiple configuration sources.
        if (argumentSection.Value != null && argumentSection.GetChildren().Any())
            throw new InvalidOperationException(
                $"The value for the argument '{argumentSection.Path}' is assigned different value " +
                "types in more than one configuration source. Ensure all configurations consistently " +
                "use either a scalar (int, string, boolean) or a complex (array, section, list, " +
                "POCO, etc.) type for this argument value.");

        if (argumentSection.Value != null)
        {
            argumentValue = new StringArgumentValue(argumentSection.Value);
        }
        else
        {
            argumentValue = new ObjectArgumentValue(argumentSection, configurationAssemblies);
        }

        return argumentValue;
    }
}
