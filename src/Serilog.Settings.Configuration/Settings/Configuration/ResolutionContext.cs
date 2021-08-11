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
        readonly IDictionary<string, LoggingFilterSwitchProxy> _declaredFilterSwitches;
        readonly IConfiguration _appConfiguration;

        public ResolutionContext(IConfiguration appConfiguration = null)
        {
            _declaredLevelSwitches = new Dictionary<string, LoggingLevelSwitch>();
            _declaredFilterSwitches = new Dictionary<string, LoggingFilterSwitchProxy>();
            _appConfiguration = appConfiguration;
        }

        /// <summary>
        /// Looks up a switch in the declared LoggingLevelSwitches
        /// </summary>
        /// <param name="switchName">the name of a switch to look up</param>
        /// <returns>the LoggingLevelSwitch registered with the name</returns>
        /// <exception cref="InvalidOperationException">if no switch has been registered with <paramref name="switchName"/></exception>
        public LoggingLevelSwitch LookUpLevelSwitchByName(string switchName)
        {
            if (_declaredLevelSwitches.TryGetValue(switchName, out var levelSwitch))
            {
                return levelSwitch;
            }

            throw new InvalidOperationException($"No LoggingLevelSwitch has been declared with name \"{switchName}\". You might be missing a section \"LevelSwitches\":{{\"{switchName}\":\"InitialLevel\"}}");
        }

        public LoggingFilterSwitchProxy LookUpFilterSwitchByName(string switchName)
        {
            if (_declaredFilterSwitches.TryGetValue(switchName, out var filterSwitch))
            {
                return filterSwitch;
            }

            throw new InvalidOperationException($"No LoggingFilterSwitch has been declared with name \"{switchName}\". You might be missing a section \"FilterSwitches\":{{\"{switchName}\":\"{{FilterExpression}}\"}}");
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
            _declaredLevelSwitches[ToSwitchReference(levelSwitchName)] = levelSwitch;
        }

        public void AddFilterSwitch(string filterSwitchName, LoggingFilterSwitchProxy filterSwitch)
        {
            if (filterSwitchName == null) throw new ArgumentNullException(nameof(filterSwitchName));
            if (filterSwitch == null) throw new ArgumentNullException(nameof(filterSwitch));
            _declaredFilterSwitches[ToSwitchReference(filterSwitchName)] = filterSwitch;
        }

        string ToSwitchReference(string switchName)
        {
            return switchName.StartsWith("$") ? switchName : $"${switchName}";
        }
    }
}
