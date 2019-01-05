using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Serilog.Core;

namespace Serilog.Settings.Configuration
{
    /// <summary>
    /// Keeps track of available elements that are useful when resolving values in the settings system.
    /// </summary>
    sealed class ResolutionContext
    {
        readonly IDictionary<string, LoggingLevelSwitch> _declaredLevelSwitches;
        readonly IConfiguration _appConfiguration;

        public ResolutionContext(IConfiguration appConfiguration = null)
        {
            _declaredLevelSwitches = new Dictionary<string, LoggingLevelSwitch>();
            _appConfiguration = appConfiguration;
        }

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

        public bool HasAppConfiguration => _appConfiguration != null;

        public IConfiguration AppConfiguration
        {
            get
            {
                if (!HasAppConfiguration)
                {
                    throw new InvalidOperationException("AppConfiguration is not available");
                }

                return _appConfiguration;
            }
        }

        public void AddLevelSwitch(string levelSwitchName, LoggingLevelSwitch levelSwitch)
        {
            if (levelSwitchName == null) throw new ArgumentNullException(nameof(levelSwitchName));
            if (levelSwitch == null) throw new ArgumentNullException(nameof(levelSwitch));
            _declaredLevelSwitches[levelSwitchName] = levelSwitch;
        }
    }
}
