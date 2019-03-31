using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

using Serilog.Configuration;
using System.Linq;

namespace Serilog.Settings.Configuration
{
    class ObjectArgumentValue : IConfigurationArgumentValue
    {
        readonly IConfigurationSection _section;
        readonly IReadOnlyCollection<Assembly> _configurationAssemblies;

        public ObjectArgumentValue(IConfigurationSection section, IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));

            // used by nested logger configurations to feed a new pass by ConfigurationReader
            _configurationAssemblies = configurationAssemblies ?? throw new ArgumentNullException(nameof(configurationAssemblies));
        }

        public object ConvertTo(Type toType, ResolutionContext resolutionContext)
        {
            // return the entire section for internal processing
            if (toType == typeof(IConfigurationSection)) return _section;

            // process a nested configuration to populate an Action<> logger/sink config parameter?
            var toTypeInfo = toType.GetTypeInfo();
            if (toTypeInfo.IsGenericType &&
                toTypeInfo.GetGenericTypeDefinition() is Type genericType && genericType == typeof(Action<>))
            {
                var configType = toTypeInfo.GenericTypeArguments[0];
                if (configType != typeof(LoggerConfiguration) && configType != typeof(LoggerSinkConfiguration))
                    throw new ArgumentException($"Configuration for Action<{configType}> is not implemented.");

                IConfigurationReader configReader = new ConfigurationReader(_section, _configurationAssemblies, resolutionContext);

                if (configType == typeof(LoggerConfiguration))
                {
                    return new Action<LoggerConfiguration>(configReader.Configure);
                }

                if (configType == typeof(LoggerSinkConfiguration))
                {
                    return new Action<LoggerSinkConfiguration>(loggerSinkConfig => configReader.ApplySinks(loggerSinkConfig));
                }
            }

            if (toTypeInfo.IsInterface || toTypeInfo.IsAbstract)
            {
                if (!_section.GetChildren().Any())
                {
                    // todo: add proper message
                    throw new InvalidOperationException();
                }

                var children = _section.GetChildren().ToList();

                // todo: add proper checking
                var parameterType = TypeHelper.FindType(children.FirstOrDefault(x => x.Key == "@type")?.Value);

                if (parameterType == null)
                {
                    throw new InvalidOperationException();
                }

                if (!toTypeInfo.IsAssignableFrom(parameterType))
                {
                    //todo: parameterType isn't assignable to to type info
                    throw new InvalidOperationException();
                }

                // todo: get all parameters with values

                var arguments = children.Where(x => x.Key != "@type").Select(x => new Tuple<string, IConfigurationSection>(x.Key, x)).ToList();
                var argumentsNames = arguments.Select(x => x.Item1).ToList();

                // todo: find a proper constructor:
                // 1. all specified parameter can be configured by one constructor

                var applicableConstructors = parameterType.GetTypeInfo().DeclaredConstructors
                    .Where(ci =>
                    {
                        var parameters = ci.GetParameters();
                        var requiredParametes = parameters.Where(p => !p.HasDefaultValue).ToList();

                        return argumentsNames.All(a => parameters.Any(p => p.Name == a)) && requiredParametes.All(p => argumentsNames.Contains(p.Name));
                    })
                    .OrderBy(ci => ci.GetParameters().Length)
                    .ToList();

                if (!applicableConstructors.Any())
                {
                    throw new InvalidOperationException();
                }

                var ctor = applicableConstructors.First();

                var call = ctor.GetParameters()
                    .Select(pi =>
                    {
                        var configuredArgument = arguments.FirstOrDefault(a => a.Item1 == pi.Name);
                        if (configuredArgument == null) return pi.DefaultValue;

                        return configuredArgument.Item2.Get(pi.ParameterType);
                        
                    })
                    .ToArray();

                return ctor.Invoke(call);

                //return _section.Get(parameterType);
            }

            // MS Config binding
            return _section.Get(toType);
        }
    }
}
