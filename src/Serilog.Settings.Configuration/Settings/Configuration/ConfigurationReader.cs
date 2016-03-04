using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Settings.Configuration
{
    public class ConfigurationReader : ILoggerSettings
    {
        readonly IConfigurationSection _configuration;
        readonly ILibraryManager _libraryManager;

        public ConfigurationReader(IConfigurationSection configuration, ILibraryManager libraryManager)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (libraryManager == null) throw new ArgumentNullException(nameof(libraryManager));
            _configuration = configuration;
            _libraryManager = libraryManager;
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            var configurationAssemblies = LoadConfigurationAssemblies();

            ApplyMinimumLevel(loggerConfiguration);

            var enrichDirective = _configuration.GetSection("Enrich");
            if (enrichDirective != null)
            {
                var withPropertiesDirective = enrichDirective.GetSection("WithProperties");
                if (withPropertiesDirective != null)
                {
                    foreach (var enrichProperyDirective in withPropertiesDirective.GetChildren())
                    {
                        loggerConfiguration.Enrich.WithProperty(enrichProperyDirective.Key, enrichProperyDirective.Value);
                    }
                }

                var withDirective = enrichDirective.GetSection("With");
                if (withDirective != null)
                {
                    var methodCalls = GetMethodCalls(withDirective);
                    CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(configurationAssemblies), loggerConfiguration.Enrich);
                }
            }

            var writeToDirective = _configuration.GetSection("WriteTo");
            if (writeToDirective != null)
            {
                var methodCalls = GetMethodCalls(writeToDirective);
                CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(configurationAssemblies), loggerConfiguration.WriteTo);
            }
        }

        Dictionary<string, Dictionary<string, string>> GetMethodCalls(IConfigurationSection directive)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var child in directive.GetChildren())
            {
                if (child.Value != null)
                {
                    // Plain string
                    result.Add(child.Value, new Dictionary<string, string>());
                }
                else
                {
                    var name = child.GetSection("Name");
                    if (name == null)
                        throw new InvalidOperationException("The configuration value in Serilog.WriteTo has no Name element.");

                    var callArgs = new Dictionary<string, string>();
                    var args = child.GetSection("Args");
                    if (args != null)
                    {
                        foreach (var argument in args.GetChildren())
                        {
                            callArgs.Add(argument.Key, Environment.ExpandEnvironmentVariables(argument.Value));
                        }
                    }
                    result.Add(name.Value, callArgs);
                }
            }
            return result;
        }

        void ApplyMinimumLevel(LoggerConfiguration loggerConfiguration)
        {
            var minimumLevelDirective = _configuration.GetSection("MinimumLevel");
            if (minimumLevelDirective != null)
            {
                LogEventLevel minimumLevel;
                if (!Enum.TryParse(minimumLevelDirective.Value, out minimumLevel))
                    throw new InvalidOperationException($"The value {minimumLevelDirective.Value} is not a valid Serilog level.");

                loggerConfiguration.MinimumLevel.Is(minimumLevel);
            }
        }

        Assembly[] LoadConfigurationAssemblies()
        {
            var assemblies = new Dictionary<AssemblyName, Assembly>();

            var usingSection = _configuration.GetSection("Using");
            if (usingSection != null)
            {
                foreach (var simpleName in usingSection.GetChildren().Select(c => c.Value))
                {
                    if (string.IsNullOrWhiteSpace(simpleName))
                        throw new InvalidOperationException(
                            "A zero-length or whitespace assembly name was supplied to a Serilog.Using configuration statement.");

                    var assembly = Assembly.Load(new AssemblyName(simpleName));
                    assemblies.Add(assembly.GetName(), assembly);
                }
            }

            foreach (var library in _libraryManager.GetLibraries())
            {
                if (library.Name != null && library.Name.ToLowerInvariant().Contains("serilog"))
                {
                    foreach (var assemblyName in library.Assemblies)
                    {
                        if (!assemblies.ContainsKey(assemblyName))
                        {
                            var assembly = Assembly.Load(assemblyName);
                            assemblies.Add(assemblyName, assembly);
                        }
                    }
                }
            }

            var configurationAssemblies = assemblies.Values.ToArray();
            return configurationAssemblies;
        }

        static void CallConfigurationMethods(Dictionary<string, Dictionary<string, string>> methods, IList<MethodInfo> configurationMethods, object receiver)
        {
            foreach (var method in methods)
            {
                var methodInfo = configurationMethods
                    .Where(m => m.Name == method.Key && m.GetParameters().Skip(1).All(p => p.HasDefaultValue || method.Value.Any(s => s.Key == p.Name)))
                    .OrderByDescending(m => m.GetParameters().Length)
                    .FirstOrDefault();

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

            var extendedTypeConversions = new Dictionary<Type, Func<string, object>>
            {
                { typeof(Uri), s => new Uri(s) },
                { typeof(TimeSpan), s => TimeSpan.Parse(s) }
            };

            var convertor = extendedTypeConversions
                .Where(t => t.Key.GetTypeInfo().IsAssignableFrom(toTypeInfo))
                .Select(t => t.Value)
                .FirstOrDefault();

            return convertor == null ? Convert.ChangeType(value, toType) : convertor(value);
        }

        internal static IList<MethodInfo> FindSinkConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            return FindConfigurationMethods(configurationAssemblies, typeof(LoggerSinkConfiguration));
        }

        // Unlike the other configuration methods, FromLogContext is an instance method rather than an extension.
        internal static LoggerConfiguration FromLogContext(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
        {
            return loggerEnrichmentConfiguration.FromLogContext();
        }

        static readonly MethodInfo SurrogateFromLogContextConfigurationMethod = typeof(ConfigurationReader)
            .GetTypeInfo()
            .DeclaredMethods.Single(m => m.Name == "FromLogContext");

        internal static IList<MethodInfo> FindEventEnricherConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerEnrichmentConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerEnrichmentConfiguration).GetTypeInfo().Assembly))
                found.Add(SurrogateFromLogContextConfigurationMethod);

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
    }
}
