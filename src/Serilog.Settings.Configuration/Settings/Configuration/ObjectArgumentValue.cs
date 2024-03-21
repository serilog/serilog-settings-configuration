using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.Extensions.Configuration;

using Serilog.Configuration;

namespace Serilog.Settings.Configuration;

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

    public object? ConvertTo(Type toType, ResolutionContext resolutionContext)
    {
        // return the entire section for internal processing
        if (toType == typeof(IConfigurationSection)) return _section;

        // process a nested configuration to populate an Action<> logger/sink config parameter?
        var typeInfo = toType.GetTypeInfo();
        if (typeInfo.IsGenericType &&
            typeInfo.GetGenericTypeDefinition() is Type genericType && genericType == typeof(Action<>))
        {
            var configType = typeInfo.GenericTypeArguments[0];
            IConfigurationReader configReader = new ConfigurationReader(_section, _configurationAssemblies, resolutionContext);

            return configType switch
            {
                _ when configType == typeof(LoggerConfiguration) => new Action<LoggerConfiguration>(configReader.Configure),
                _ when configType == typeof(LoggerSinkConfiguration) => new Action<LoggerSinkConfiguration>(configReader.ApplySinks),
                _ when configType == typeof(LoggerEnrichmentConfiguration) => new Action<LoggerEnrichmentConfiguration>(configReader.ApplyEnrichment),
                _ => throw new ArgumentException($"Configuration resolution for Action<{configType.Name}> parameter type at the path {_section.Path} is not implemented.")
            };
        }

        if (toType.IsArray)
            return CreateArray();

        // Only try to call ctor when type is explicitly specified in _section
        if (TryCallCtorExplicit(_section, resolutionContext, out var ctorResult))
            return ctorResult;

        if (IsContainer(toType, out var elementType) && TryCreateContainer(out var container))
            return container;

        // Without a type explicitly specified, attempt to call ctor of toType
        if (TryCallCtorImplicit(_section, toType, resolutionContext, out ctorResult))
            return ctorResult;

        // MS Config binding can work with a limited set of primitive types and collections
        return _section.Get(toType);

        object CreateArray()
        {
            var arrayElementType = toType.GetElementType()!;
            var configurationElements = _section.GetChildren().ToArray();
            var array = Array.CreateInstance(arrayElementType, configurationElements.Length);
            for (int i = 0; i < configurationElements.Length; ++i)
            {
                var argumentValue = ConfigurationReader.GetArgumentValue(configurationElements[i], _configurationAssemblies);
                var value = argumentValue.ConvertTo(arrayElementType, resolutionContext);
                array.SetValue(value, i);
            }

            return array;
        }

        bool TryCreateContainer([NotNullWhen(true)] out object? result)
        {
            result = null;

            if (IsConstructableDictionary(toType, elementType, out var concreteType, out var keyType, out var valueType, out var addMethod))
            {
                result = Activator.CreateInstance(concreteType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {concreteType}");

                foreach (var section in _section.GetChildren())
                {
                    var argumentValue = ConfigurationReader.GetArgumentValue(section, _configurationAssemblies);
                    var key = new StringArgumentValue(section.Key).ConvertTo(keyType, resolutionContext);
                    var value = argumentValue.ConvertTo(valueType, resolutionContext);
                    addMethod.Invoke(result, new[] { key, value });
                }
                return true;
            }
            else if (IsConstructableContainer(toType, elementType, out concreteType, out addMethod))
            {
                result = Activator.CreateInstance(concreteType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {concreteType}");

                foreach (var section in _section.GetChildren())
                {
                    var argumentValue = ConfigurationReader.GetArgumentValue(section, _configurationAssemblies);
                    var value = argumentValue.ConvertTo(elementType, resolutionContext);
                    addMethod.Invoke(result, new[] { value });
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    bool TryCallCtorExplicit(
        IConfigurationSection section, ResolutionContext resolutionContext, [NotNullWhen(true)] out object? value)
    {
        var typeDirective = section.GetValue<string>("$type") switch
        {
            not null => "$type",
            null => section.GetValue<string>("type") switch
            {
                not null => "type",
                null => null,
            },
        };

        var type = typeDirective switch
        {
            not null => Type.GetType(section.GetValue<string>(typeDirective)!, throwOnError: false),
            null => null,
        };

        if (type is null or { IsAbstract: true })
        {
            value = null;
            return false;
        }
        else
        {
            var suppliedArguments = section.GetChildren().Where(s => s.Key != typeDirective)
                .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
            return TryCallCtor(type, suppliedArguments, resolutionContext, out value);
        }

    }

    bool TryCallCtorImplicit(
        IConfigurationSection section, Type parameterType, ResolutionContext resolutionContext, out object? value)
    {
        var suppliedArguments = section.GetChildren()
            .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        return TryCallCtor(parameterType, suppliedArguments, resolutionContext, out value);
    }

    bool TryCallCtor(Type type, Dictionary<string, IConfigurationSection> suppliedArguments, ResolutionContext resolutionContext, [NotNullWhen(true)] out object? value)
    {
        value = null;

        if (suppliedArguments.Count == 0 &&
            type.GetConstructor(Type.EmptyTypes) is ConstructorInfo parameterlessCtor)
        {
            value = parameterlessCtor.Invoke([]);
            return true;
        }

        var ctor =
            (from c in type.GetConstructors()
             from p in c.GetParameters()
             let argumentBindResult = suppliedArguments.TryGetValue(p.Name ?? "", out var argValue) switch
             {
                 true => new { success = true, hasMatch = true, value = (object?)argValue },
                 false => p.HasDefaultValue switch
                 {
                     true  => new { success = true,  hasMatch = false, value = (object?)p.DefaultValue },
                     false => new { success = false, hasMatch = false, value = (object?)null },
                 },
             }
             group new { argumentBindResult, p.ParameterType } by c into gr
             where gr.All(z => z.argumentBindResult.success)
             let matchedArgs = gr.Where(z => z.argumentBindResult.hasMatch).ToList()
             orderby matchedArgs.Count descending,
                     matchedArgs.Count(p => p.ParameterType == typeof(string)) descending
             select new
             {
                 ConstructorInfo = gr.Key,
                 ArgumentValues = gr.Select(z => new { Value = z.argumentBindResult.value, Type = z.ParameterType })
                                    .ToList()
             }).FirstOrDefault();

        if (ctor is null)
        {
            return false;
        }

        var ctorArguments = new object?[ctor.ArgumentValues.Count];
        for (var i = 0; i < ctor.ArgumentValues.Count; i++)
        {
            var argument = ctor.ArgumentValues[i];
            var valueValue = argument.Value;
            if (valueValue is IConfigurationSection s)
            {
                var argumentValue = ConfigurationReader.GetArgumentValue(s, _configurationAssemblies);
                valueValue = argumentValue.ConvertTo(argument.Type, resolutionContext);
            }
            ctorArguments[i] = valueValue;
        }

        value = ctor.ConstructorInfo.Invoke(ctorArguments);
        return true;
    }

    static bool IsContainer(Type type, [NotNullWhen(true)] out Type? elementType)
    {
        elementType = null;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }
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

    static bool IsConstructableDictionary(Type type, Type elementType, [NotNullWhen(true)] out Type? concreteType, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valueType, [NotNullWhen(true)] out MethodInfo? addMethod)
    {
        concreteType = null;
        keyType = null;
        valueType = null;
        addMethod = null;
        if (!elementType.IsGenericType || elementType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
        {
            return false;
        }
        var argumentTypes = elementType.GetGenericArguments();
        keyType = argumentTypes[0];
        valueType = argumentTypes[1];
        if (type.IsAbstract)
        {
            concreteType = typeof(Dictionary<,>).MakeGenericType(argumentTypes);
            if (!type.IsAssignableFrom(concreteType))
            {
                return false;
            }
        }
        else
        {
            concreteType = type;
        }
        if (concreteType.GetConstructor(Type.EmptyTypes) == null)
        {
            return false;
        }
        foreach (var method in concreteType.GetMethods())
        {
            if (!method.IsStatic && method.Name == "Add")
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == keyType && parameters[1].ParameterType == valueType)
                {
                    addMethod = method;
                    return true;
                }
            }
        }
        return false;
    }

    static bool IsConstructableContainer(Type type, Type elementType, [NotNullWhen(true)] out Type? concreteType, [NotNullWhen(true)] out MethodInfo? addMethod)
    {
        addMethod = null;
        if (type.IsAbstract)
        {
            concreteType = typeof(List<>).MakeGenericType(elementType);
            if (!type.IsAssignableFrom(concreteType))
            {
                concreteType = typeof(HashSet<>).MakeGenericType(elementType);
                if (!type.IsAssignableFrom(concreteType))
                {
                    concreteType = null;
                    return false;
                }
            }
        }
        else
        {
            concreteType = type;
        }
        if (concreteType.GetConstructor(Type.EmptyTypes) == null)
        {
            return false;
        }
        foreach (var method in concreteType.GetMethods())
        {
            if (!method.IsStatic && method.Name == "Add")
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == elementType)
                {
                    addMethod = method;
                    return true;
                }
            }
        }
        return false;
    }
}
