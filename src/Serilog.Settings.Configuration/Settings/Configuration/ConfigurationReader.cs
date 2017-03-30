﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Primitives;

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Linq.Expressions;

namespace Serilog.Settings.Configuration
{
    class ConfigurationReader : ILoggerSettings
    {
        readonly IConfigurationSection _configuration;
        readonly DependencyContext _dependencyContext;

        public ConfigurationReader(IConfigurationSection configuration, DependencyContext dependencyContext)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;
            _dependencyContext = dependencyContext;
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            var configurationAssemblies = LoadConfigurationAssemblies();

            ApplyMinimumLevel(loggerConfiguration);
            ApplyEnrichment(loggerConfiguration, configurationAssemblies);
            ApplyFilters(loggerConfiguration, configurationAssemblies);
            ApplySinks(loggerConfiguration, configurationAssemblies);
        }

        void ApplyFilters(LoggerConfiguration loggerConfiguration, Assembly[] configurationAssemblies)
        {
            var filterDirective = _configuration.GetSection("Filter");
            if (filterDirective != null)
            {
                var methodCalls = GetMethodCalls(filterDirective);
                CallConfigurationMethods(methodCalls, FindFilterConfigurationMethods(configurationAssemblies), loggerConfiguration.Filter);
            }
        }

        void ApplySinks(LoggerConfiguration loggerConfiguration, Assembly[] configurationAssemblies)
        {
            var writeToDirective = _configuration.GetSection("WriteTo");
            if (writeToDirective != null)
            {
                var methodCalls = GetMethodCalls(writeToDirective);
                CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(configurationAssemblies),
                    loggerConfiguration.WriteTo);
            }
        }

        void ApplyEnrichment(LoggerConfiguration loggerConfiguration, Assembly[] configurationAssemblies)
        {
            var enrichDirective = _configuration.GetSection("Enrich");
            if (enrichDirective != null)
            {
                var methodCalls = GetMethodCalls(enrichDirective);
                CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(configurationAssemblies),
                    loggerConfiguration.Enrich);
            }

            var propertiesDirective = _configuration.GetSection("Properties");
            if (propertiesDirective != null)
            {
                foreach (var enrichProperyDirective in propertiesDirective.GetChildren())
                {
                    loggerConfiguration.Enrich.WithProperty(enrichProperyDirective.Key, enrichProperyDirective.Value);
                }
            }
        }

        void ApplyMinimumLevel(LoggerConfiguration loggerConfiguration)
        {
            var applyMinimumLevelAction =
                new Action<IConfigurationSection, Action<LoggerMinimumLevelConfiguration, LoggingLevelSwitch>>(
                    (directive, applyConfigAction) =>
                    {
                        LogEventLevel minimumLevel;
                        if (!Enum.TryParse(directive.Value, out minimumLevel))
                            throw new InvalidOperationException($"The value {directive.Value} is not a valid Serilog level.");

                        var levelSwitch = new LoggingLevelSwitch(minimumLevel);
                        applyConfigAction(loggerConfiguration.MinimumLevel, levelSwitch);

                        ChangeToken.OnChange(
                            directive.GetReloadToken,
                            () =>
                            {
                                if (Enum.TryParse(directive.Value, out minimumLevel))
                                    levelSwitch.MinimumLevel = minimumLevel;
                                else
                                    SelfLog.WriteLine($"The value {directive.Value} is not a valid Serilog level.");
                            });
                    });

            var minimumLevelDirective = _configuration.GetSection("MinimumLevel");

            var defaultMinLevelDirective = minimumLevelDirective.Value != null ? minimumLevelDirective : minimumLevelDirective.GetSection("Default");
            if (defaultMinLevelDirective.Value != null)
            {
                applyMinimumLevelAction(defaultMinLevelDirective, (configuration, levelSwitch) => configuration.ControlledBy(levelSwitch));
            }

            foreach (var overrideDirective in minimumLevelDirective.GetSection("Override").GetChildren())
            {
                applyMinimumLevelAction(overrideDirective, (configuration, levelSwitch) => configuration.Override(overrideDirective.Key, levelSwitch));
            }
        }

        Assembly[] LoadConfigurationAssemblies()
        {
            var assemblies = new Dictionary<string, Assembly>();

            var usingSection = _configuration.GetSection("Using");
            if (usingSection != null)
            {
                foreach (var simpleName in usingSection.GetChildren().Select(c => c.Value))
                {
                    if (string.IsNullOrWhiteSpace(simpleName))
                        throw new InvalidOperationException(
                            "A zero-length or whitespace assembly name was supplied to a Serilog.Using configuration statement.");

                    var assembly = Assembly.Load(new AssemblyName(simpleName));

                    if (!assemblies.ContainsKey(assembly.FullName))
                        assemblies.Add(assembly.FullName, assembly);
                }
            }

            foreach (var assemblyName in GetSerilogConfigurationAssemblies())
            {
                var assumed = Assembly.Load(assemblyName);
                if (assumed != null && !assemblies.ContainsKey(assumed.FullName))
                    assemblies.Add(assumed.FullName, assumed);
            }

            return assemblies.Values.ToArray();
        }

        AssemblyName[] GetSerilogConfigurationAssemblies()
        {
            var query = Enumerable.Empty<AssemblyName>();
            var filter = new Func<string, bool>(name => name != null && name.ToLowerInvariant().Contains("serilog"));

            if (_dependencyContext != null)
            {
                query = from library in _dependencyContext.RuntimeLibraries
                        from assemblyName in library.GetDefaultAssemblyNames(_dependencyContext)
                        where filter(assemblyName.Name)
                        select assemblyName;
            }
            else
            {
#if APPDOMAIN
                query = from outputAssemblyPath in System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                        let assemblyFileName = System.IO.Path.GetFileNameWithoutExtension(outputAssemblyPath)
                        where filter(assemblyFileName)
                        select AssemblyName.GetAssemblyName(outputAssemblyPath);
#endif
            }

            return query.ToArray();
        }

        internal static ILookup<string, Dictionary<string, string>> GetMethodCalls(IConfigurationSection directive)
        {
            var nameParser =
                new Func<IConfigurationSection, string>(
                    dir =>
                    {
                        var name = dir.GetSection("Name");
                        if (name.Value == null)
                            throw new InvalidOperationException($"The configuration value in {name.Path} has no Name element.");

                        return name.Value;
                    });

            var children = directive.GetChildren();

            var result =
                (from child in children
                 where child.Value != null // Plain string
                 select new { Name = child.Value, Args = new Dictionary<string, string>() })
                     .Concat(
                (from child in children
                 where child.Value == null
                 let name = nameParser(child)
                 let callArgs = (from argument in child.GetSection("Args").GetChildren()
                                 select new { Name = argument.Key, Value = Environment.ExpandEnvironmentVariables(argument.Value) }).ToDictionary(p => p.Name, p => p.Value)
                 select new { Name = name, Args = callArgs }))
                     .ToLookup(p => p.Name, p => p.Args);

            return result;
        }

        static void CallConfigurationMethods(ILookup<string, Dictionary<string, string>> methods, IList<MethodInfo> configurationMethods, object receiver)
        {
            foreach (var method in methods.SelectMany(g => g.Select(x => new { g.Key, Value = x })))
            {
                var methodInfo = SelectConfigurationMethod(configurationMethods, method.Key, method.Value);

                if (methodInfo != null)
                {
                    var call = (from p in methodInfo.GetParameters().Skip(1)
                                let directive = method.Value.FirstOrDefault(s => s.Key == p.Name)
                                select directive.Key == null ? p.DefaultValue : ConvertToType(directive.Value, p.ParameterType)).ToList();

                    call.Insert(0, receiver);

                    methodInfo.Invoke(null, call.ToArray());
                }
            }
        }

        internal static MethodInfo SelectConfigurationMethod(IEnumerable<MethodInfo> candidateMethods, string name, Dictionary<string, string> suppliedArgumentValues)
        {
            return candidateMethods
                .Where(m => m.Name == name &&
                            m.GetParameters().Skip(1).All(p => p.HasDefaultValue || suppliedArgumentValues.Any(s => s.Key == p.Name)))
                .OrderByDescending(m => m.GetParameters().Count(p => suppliedArgumentValues.Any(s => s.Key == p.Name)))
                .FirstOrDefault();
        }

        static readonly Dictionary<Type, Func<string, object>> ExtendedTypeConversions = new Dictionary<Type, Func<string, object>>
            {
                { typeof(Uri), s => new Uri(s) },
                { typeof(TimeSpan), s => TimeSpan.Parse(s) }
            };


        internal static object ConvertToType(string value, Type toType)
        {
            var toTypeInfo = toType.GetTypeInfo();
            if (toTypeInfo.IsGenericType && toType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                // unwrap Nullable<> type since we're not handling null situations
                toType = toTypeInfo.GenericTypeArguments[0];
                toTypeInfo = toType.GetTypeInfo();
            }

            if (toTypeInfo.IsEnum)
                return Enum.Parse(toType, value);

            var convertor = ExtendedTypeConversions
                .Where(t => t.Key.GetTypeInfo().IsAssignableFrom(toTypeInfo))
                .Select(t => t.Value)
                .FirstOrDefault();

            if (convertor != null)
                 return convertor(value);
 
            if (toTypeInfo.IsInterface && !string.IsNullOrWhiteSpace(value))
            {
                var type = Type.GetType(value.Trim(), throwOnError: false);
                if (type != null)
                {
                    var ctor = type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(ci =>
                    {
                        var parameters = ci.GetParameters();
                        return parameters.Length == 0 || parameters.All(pi => pi.HasDefaultValue);
                    });
 
                    if (ctor == null)
                        throw new InvalidOperationException($"A default constructor was not found on {type.FullName}.");
 
                    var call = ctor.GetParameters().Select(pi => pi.DefaultValue).ToArray();
                    return ctor.Invoke(call);
                }
            }

            return Convert.ChangeType(value, toType);
        }

        internal static IList<MethodInfo> FindSinkConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            return FindConfigurationMethods(configurationAssemblies, typeof(LoggerSinkConfiguration));
        }

        internal static IList<MethodInfo> FindFilterConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerFilterConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerFilterConfiguration).GetTypeInfo().Assembly))
                found.Add(GetSurrogateConfigurationMethod<LoggerFilterConfiguration, ILogEventFilter>((c, f) => With(c, f)));

            return found;
        }

        internal static IList<MethodInfo> FindEventEnricherConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerEnrichmentConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerEnrichmentConfiguration).GetTypeInfo().Assembly))
                found.Add(GetSurrogateConfigurationMethod<LoggerEnrichmentConfiguration, object>((c, _) => FromLogContext(c)));

            return found;
        }

        internal static IList<MethodInfo> FindConfigurationMethods(IEnumerable<Assembly> configurationAssemblies, Type configType)
        {
            return configurationAssemblies
                .SelectMany(a => a.ExportedTypes
                    .Select(t => t.GetTypeInfo())
                    .Where(t => t.IsSealed && t.IsAbstract && !t.IsNested))
                .SelectMany(t => t.DeclaredMethods)
                .Where(m => m.IsStatic && m.IsPublic && m.IsDefined(typeof(ExtensionAttribute), false))
                .Where(m => m.GetParameters()[0].ParameterType == configType)
                .ToList();
        }

        // don't support (yet?) arrays in the parameter list (ILogEventEnricher[])
        internal static LoggerConfiguration With(LoggerFilterConfiguration loggerFilterConfiguration, ILogEventFilter filter)
        {
            return loggerFilterConfiguration.With(filter);
        }

        // Unlike the other configuration methods, FromLogContext is an instance method rather than an extension.
        internal static LoggerConfiguration FromLogContext(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
        {
            return loggerEnrichmentConfiguration.FromLogContext();
        }

        internal static MethodInfo GetSurrogateConfigurationMethod<TConfiguration, TArg>(Expression<Action<TConfiguration, TArg>> method)
        {
            return (method.Body as MethodCallExpression)?.Method;
        }
    }
}
