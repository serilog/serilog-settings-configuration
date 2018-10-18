using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Serilog.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Serilog.Settings.Configuration
{
    class ObjectArgumentValue : IConfigurationArgumentValue
    {
        readonly IConfigurationSection section;
        readonly IReadOnlyCollection<Assembly> configurationAssemblies;
        readonly DependencyContext dependencyContext;

        public ObjectArgumentValue(IConfigurationSection section, IReadOnlyCollection<Assembly> configurationAssemblies, DependencyContext dependencyContext)
        {
            this.section = section;

            // used by nested logger configurations to feed a new pass by ConfigurationReader
            this.configurationAssemblies = configurationAssemblies;
            this.dependencyContext = dependencyContext;
        }

        public object ConvertTo(Type toType, SettingValueResolver valueResolver)
        {
            // return the entire section for internal processing
            if (toType == typeof(IConfigurationSection)) return section;

            // process a nested configuration to populate an Action<> logger/sink config parameter?
            var typeInfo = toType.GetTypeInfo();
            if (typeInfo.IsGenericType &&
                typeInfo.GetGenericTypeDefinition() is Type genericType && genericType == typeof(Action<>))
            {
                var configType = typeInfo.GenericTypeArguments[0];
                if (configType != typeof(LoggerConfiguration) && configType != typeof(LoggerSinkConfiguration))
                    throw new ArgumentException($"Configuration for Action<{configType}> is not implemented.");

                IConfigurationReader configReader = new ConfigurationReader(section, configurationAssemblies, dependencyContext, valueResolver);

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
            return section.Get(toType);
        }
    }
}
