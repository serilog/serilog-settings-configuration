

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog.Core;

namespace Serilog.Settings.Configuration
{
    internal sealed class SettingValueResolver
    {
        readonly IReadOnlyDictionary<string, LoggingLevelSwitch> _declaredLevelSwitches;

        public SettingValueResolver(IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches, IConfiguration appConfiguration)
        {
            _declaredLevelSwitches = declaredLevelSwitches ?? throw new ArgumentNullException(nameof(declaredLevelSwitches));
            AppConfiguration = appConfiguration;
        }

        public IConfiguration AppConfiguration { get; }

        /// <summary>
        /// Looks up a switch in the declared LoggingLevelSwitches
        /// </summary>
        /// <param name="switchName">the name of a switch to look up</param>
        /// <returns>the LoggingLevelSwitch registered with the name</returns>
        /// <exception cref="InvalidOperationException">if no switch has been registered with <paramref name="switchName"/></exception>
        public LoggingLevelSwitch LookUpSwitchByName(string switchName)
        {
            if (_declaredLevelSwitches.TryGetValue(switchName, out var levelSwitch))
            {
                return levelSwitch;
            }

            throw new InvalidOperationException($"No LoggingLevelSwitch has been declared with name \"{switchName}\". You might be missing a section \"LevelSwitches\":{{\"{switchName}\":\"InitialLevel\"}}");
        }

        public static SettingValueResolver Default()
        {
            return new SettingValueResolver(new Dictionary<string, LoggingLevelSwitch>(), null);
        }
    }
}
