using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Configuration;

using Serilog.Configuration;

namespace Serilog.Settings.Configuration;

[RequiresUnreferencedCode(TrimWarningMessages.UnboundedReflection)]
[RequiresDynamicCode(TrimWarningMessages.CreatesArraysOfArbitraryTypes)]
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

        if (IsContainer(toType, out var elementType) && TryCreateContainer(out var container))
            return container;

        if (TryBuildCtorExpression(_section, toType, resolutionContext, out var ctorExpression))
        {
            return Expression.Lambda<Func<object>>(ctorExpression).Compile().Invoke();
        }

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

            if (toType.GetConstructor(Type.EmptyTypes) == null)
                return false;

            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers#collection-initializers
            var addMethod = toType.GetMethods().FirstOrDefault(m => !m.IsStatic && m.Name == "Add" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == elementType);
            if (addMethod == null)
                return false;

            var configurationElements = _section.GetChildren().ToArray();
            result = Activator.CreateInstance(toType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {toType}");

            for (int i = 0; i < configurationElements.Length; ++i)
            {
                var argumentValue = ConfigurationReader.GetArgumentValue(configurationElements[i], _configurationAssemblies);
                var value = argumentValue.ConvertTo(elementType, resolutionContext);
                addMethod.Invoke(result, new[] { value });
            }

            return true;
        }
    }

    [RequiresUnreferencedCode(TrimWarningMessages.UnboundedReflection)]
    internal static bool TryBuildCtorExpression(
        IConfigurationSection section, Type parameterType, ResolutionContext resolutionContext, [NotNullWhen(true)] out NewExpression? ctorExpression)
    {
        ctorExpression = null;

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
            not null => Type.GetType(section.GetValue<string>(typeDirective), throwOnError: false),
            null => parameterType,
        };

        if (type is null or { IsAbstract: true })
        {
            return false;
        }

        var suppliedArguments = section.GetChildren().Where(s => s.Key != typeDirective)
            .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        if (suppliedArguments.Count == 0 &&
            type.GetConstructor(Type.EmptyTypes) is ConstructorInfo parameterlessCtor)
        {
            ctorExpression = Expression.New(parameterlessCtor);
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

        var ctorArguments = new List<Expression>();
        foreach (var argumentValue in ctor.ArgumentValues)
        {
            if (TryBindToCtorArgument(argumentValue.Value, argumentValue.Type, resolutionContext, out var argumentExpression))
            {
                ctorArguments.Add(argumentExpression);
            }
            else
            {
                return false;
            }
        }

        ctorExpression = Expression.New(ctor.ConstructorInfo, ctorArguments);
        return true;

        static bool TryBindToCtorArgument(object value, Type type, ResolutionContext resolutionContext, [NotNullWhen(true)] out Expression? argumentExpression)
        {
            argumentExpression = null;

            if (value is IConfigurationSection s)
            {
                if (s.Value is string argValue)
                {
                    var stringArgumentValue = new StringArgumentValue(argValue);
                    try
                    {
                        argumentExpression = Expression.Constant(
                            stringArgumentValue.ConvertTo(type, resolutionContext),
                            type);

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else if (s.GetChildren().Any())
                {
                    if (TryBuildCtorExpression(s, type, resolutionContext, out var ctorExpression))
                    {
                        argumentExpression = ctorExpression;
                        return true;
                    }

                    return false;
                }
            }

            argumentExpression = Expression.Constant(value, type);
            return true;
        }
    }

    static bool IsContainer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        [NotNullWhen(true)] out Type? elementType)
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
