using Microsoft.Extensions.Configuration;
using Serilog.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            var typeInfo = toType.GetTypeInfo();
            if (typeInfo.IsGenericType &&
                typeInfo.GetGenericTypeDefinition() is Type genericType && genericType == typeof(Action<>))
            {
                var configType = typeInfo.GenericTypeArguments[0];
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

            if (toType.IsArray)
                return CreateArray();

            if (IsContainer(toType, out var elementType) && TryCreateContainer(out var result))
                return result;

            // MS Config binding can work with a limited set of primitive types and collections
            return _section.Get(toType);

            object CreateArray()
            {
                var elementType = toType.GetElementType();
                var configurationElements = _section.GetChildren().ToArray();
                var result = Array.CreateInstance(elementType, configurationElements.Length);
                for (int i = 0; i < configurationElements.Length; ++i)
                {
                    var argumentValue = ConfigurationReader.GetArgumentValue(configurationElements[i], _configurationAssemblies);
                    var value = argumentValue.ConvertTo(elementType, resolutionContext);
                    result.SetValue(value, i);
                }

                return result;
            }

            bool TryCreateContainer(out object result)
            {
                result = null;

                if (toType.GetConstructor(Type.EmptyTypes) == null)
                    return false;

                // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers#collection-initializers
                var addMethod = toType.GetMethods().FirstOrDefault(m => !m.IsStatic && m.Name == "Add" && m.GetParameters()?.Length == 1 && m.GetParameters()[0].ParameterType == elementType);
                if (addMethod == null)
                    return false;

                var configurationElements = _section.GetChildren().ToArray();
                result = Activator.CreateInstance(toType);
                
                for (int i = 0; i < configurationElements.Length; ++i)
                {
                    var argumentValue = ConfigurationReader.GetArgumentValue(configurationElements[i], _configurationAssemblies);
                    var value = argumentValue.ConvertTo(elementType, resolutionContext);
                    addMethod.Invoke(result, new object[] { value });
                }

                return true;
            }
        }

        private static bool IsContainer(Type type, out Type elementType)
        {
            elementType = null;
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = iface.GetGenericArguments()[0];
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
