using System;
using System.Collections.Generic;
using Serilog.Core;

namespace Serilog.Settings.Configuration
{
    internal static class LevelSwitchDictionaryExtensions
    {
        /// <summary>
        /// Looks up a switch in the declared LoggingLevelSwitches
        /// </summary>
        /// <param name="namedLevelSwitches">the dictionary of switches to look up by name</param>
        /// <param name="switchName">the name of a switch to look up</param>
        /// <returns>the LoggingLevelSwitch registered with the name</returns>
        /// <exception cref="InvalidOperationException">if no switch has been registered with <paramref name="switchName"/></exception>
        public static LoggingLevelSwitch LookUpSwitchByName(this IReadOnlyDictionary<string, LoggingLevelSwitch> namedLevelSwitches, string switchName)
        {
            if (namedLevelSwitches == null) throw new ArgumentNullException(nameof(namedLevelSwitches));
            if (namedLevelSwitches.TryGetValue(switchName, out var levelSwitch))
            {
                return levelSwitch;
            }

            throw new InvalidOperationException($"No LoggingLevelSwitch has been declared with name \"{switchName}\". You might be missing a section \"LevelSwitches\":{{\"{switchName}\":\"InitialLevel\"}}");
        }
    }
}
