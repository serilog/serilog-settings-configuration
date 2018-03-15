using System.Text.RegularExpressions;

namespace Serilog.Settings.Configuration
{
    static class ConfigurationValueSyntax
    {
        const string LevelSwitchNameRegex = @"^\$[A-Za-z]+[A-Za-z0-9]*$";

        public static bool IsValidSwitchName(string input)
        {
            return Regex.IsMatch(input, LevelSwitchNameRegex);
        }
    }
}