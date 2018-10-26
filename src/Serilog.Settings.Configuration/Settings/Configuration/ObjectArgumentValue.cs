using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

using Serilog.Configuration;
using Serilog.Settings.Configuration.Assemblies;

namespace Serilog.Settings.Configuration
{
    class ObjectArgumentValue : IConfigurationArgumentValue
    {
        readonly IConfigurationSection _section;
        readonly IReadOnlyCollection<Assembly> _configurationAssemblies;
        readonly AssemblyFinder _assemblyFinder;

        public ObjectArgumentValue(IConfigurationSection section, IReadOnlyCollection<Assembly> configurationAssemblies, AssemblyFinder assemblyFinder)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));

            // used by nested logger configurations to feed a new pass by ConfigurationReader
            _configurationAssemblies = configurationAssemblies ?? throw new ArgumentNullException(nameof(configurationAssemblies));
            _assemblyFinder = assemblyFinder ?? throw new ArgumentNullException(nameof(assemblyFinder));
        }

        public object ConvertTo(Type toType, SettingValueResolver valueResolver)
        {
            // return the entire section for internal processing
            if (toType == typeof(IConfigurationSection)) return _section;

            // process a nested configuration to populate an Action<> logger/sink config parameter?
            var typeInfo = toType.GetTypeInfo();
            if (typeInfo.IsGenericType &&
                typeInfo.GetGenericTypeDefinition() is Type genericType && genericType == typeof(Action<>))
            {
                var configType = typeInfo.GenericTypeArguments[0];
                if (configType != typeof(LoggerConfiguration) && configType != typeof(LoggerSinkConfiguration))
                    throw new ArgumentException($"Configuration for Action<{configType}> is not implemented.");

                IConfigurationReader configReader = new ConfigurationReader(_section, _configurationAssemblies, _assemblyFinder, valueResolver);

                if (configType == typeof(LoggerConfiguration))
                {
                    return new Action<LoggerConfiguration>(configReader.Configure);
                }

                if (configType == typeof(LoggerSinkConfiguration))
                {
                    return new Action<LoggerSinkConfiguration>(loggerSinkConfig => configReader.ApplySinks(loggerSinkConfig, valueResolver));
                }
            }

            // MS Config binding
            return _section.Get(toType);
        }
    }
}
