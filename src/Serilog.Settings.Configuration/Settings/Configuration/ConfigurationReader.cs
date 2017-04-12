using System;
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
    class ConfigurationReader : IConfigurationReader
    {
        readonly IConfigurationSection _configuration;
        readonly DependencyContext _dependencyContext;
        readonly Assembly[] _configurationAssemblies;

        public ConfigurationReader(IConfigurationSection configuration, DependencyContext dependencyContext)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dependencyContext = dependencyContext;
            _configurationAssemblies = LoadConfigurationAssemblies();
        }

        ConfigurationReader(IConfigurationSection configuration, Assembly[] configurationAssemblies, DependencyContext dependencyContext)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dependencyContext = dependencyContext;
            _configurationAssemblies = configurationAssemblies ?? throw new ArgumentNullException(nameof(configurationAssemblies));
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            ApplyMinimumLevel(loggerConfiguration);
            ApplyEnrichment(loggerConfiguration);
            ApplyFilters(loggerConfiguration);
            ApplySinks(loggerConfiguration);
        }        

        void ApplyMinimumLevel(LoggerConfiguration loggerConfiguration)
        {
            var minimumLevelDirective = _configuration.GetSection("MinimumLevel");

            var defaultMinLevelDirective = minimumLevelDirective.Value != null ? minimumLevelDirective : minimumLevelDirective.GetSection("Default");
            if (defaultMinLevelDirective.Value != null)
            {
                ApplyMinimumLevel(defaultMinLevelDirective, (configuration, levelSwitch) => configuration.ControlledBy(levelSwitch));
            }

            foreach (var overrideDirective in minimumLevelDirective.GetSection("Override").GetChildren())
            {
                ApplyMinimumLevel(overrideDirective, (configuration, levelSwitch) => configuration.Override(overrideDirective.Key, levelSwitch));
            }

            void ApplyMinimumLevel(IConfigurationSection directive, Action<LoggerMinimumLevelConfiguration, LoggingLevelSwitch> applyConfigAction)
            {
                if (!Enum.TryParse(directive.Value, out LogEventLevel minimumLevel))
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
            }
        }

        void ApplyFilters(LoggerConfiguration loggerConfiguration)
        {
            var filterDirective = _configuration.GetSection("Filter");
            if (filterDirective != null)
            {
                var methodCalls = GetMethodCalls(filterDirective);
                CallConfigurationMethods(methodCalls, FindFilterConfigurationMethods(_configurationAssemblies), loggerConfiguration.Filter);
            }
        }

        void ApplySinks(LoggerConfiguration loggerConfiguration)
        {
            var writeToDirective = _configuration.GetSection("WriteTo");
            if (writeToDirective != null)
            {
                var methodCalls = GetMethodCalls(writeToDirective);
                CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerConfiguration.WriteTo);
            }
        }

        void IConfigurationReader.ApplySinks(LoggerSinkConfiguration loggerSinkConfiguration)
        {
            var methodCalls = GetMethodCalls(_configuration);
            CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerSinkConfiguration);
        }

        void ApplyEnrichment(LoggerConfiguration loggerConfiguration)
        {
            var enrichDirective = _configuration.GetSection("Enrich");
            if (enrichDirective != null)
            {
                var methodCalls = GetMethodCalls(enrichDirective);
                CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(_configurationAssemblies), loggerConfiguration.Enrich);
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

        internal ILookup<string, Dictionary<string, IConfigurationArgumentValue>> GetMethodCalls(IConfigurationSection directive)
        {
            var children = directive.GetChildren();

            var result =
                (from child in children
                 where child.Value != null // Plain string
                 select new { Name = child.Value, Args = new Dictionary<string, IConfigurationArgumentValue>() })
                     .Concat(
                (from child in children
                 where child.Value == null
                 let name = GetSectionName(child)
                 let callArgs = (from argument in child.GetSection("Args").GetChildren()
                                 select new {
                                     Name = argument.Key,
                                     Value = GetArgumentValue(argument) }).ToDictionary(p => p.Name, p => p.Value)
                 select new { Name = name, Args = callArgs }))
                     .ToLookup(p => p.Name, p => p.Args);

            return result;

            IConfigurationArgumentValue GetArgumentValue(IConfigurationSection argumentSection)
            {
                IConfigurationArgumentValue argumentValue;
                if (argumentSection.Value != null)
                {
                    argumentValue = new StringArgumentValue(() => argumentSection.Value, argumentSection.GetReloadToken);
                }
                else
                {
                    argumentValue = new ConfigurationSectionArgumentValue(new ConfigurationReader(argumentSection, _configurationAssemblies, _dependencyContext));
                }

                return argumentValue;
            }

            string GetSectionName(IConfigurationSection s)
            {
                var name = s.GetSection("Name");
                if (name.Value == null)
                    throw new InvalidOperationException($"The configuration value in {name.Path} has no 'Name' element.");

                return name.Value;
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

        static void CallConfigurationMethods(ILookup<string, Dictionary<string, IConfigurationArgumentValue>> methods, IList<MethodInfo> configurationMethods, object receiver)
        {
            foreach (var method in methods.SelectMany(g => g.Select(x => new { g.Key, Value = x })))
            {
                var methodInfo = SelectConfigurationMethod(configurationMethods, method.Key, method.Value);

                if (methodInfo != null)
                {
                    var call = (from p in methodInfo.GetParameters().Skip(1)
                                let directive = method.Value.FirstOrDefault(s => s.Key == p.Name)
                                select directive.Key == null ? p.DefaultValue : directive.Value.ConvertTo(p.ParameterType)).ToList();

                    call.Insert(0, receiver);

                    methodInfo.Invoke(null, call.ToArray());
                }
            }
        }

        internal static MethodInfo SelectConfigurationMethod(IEnumerable<MethodInfo> candidateMethods, string name, Dictionary<string, IConfigurationArgumentValue> suppliedArgumentValues)
        {
            return candidateMethods
                .Where(m => m.Name == name &&
                            m.GetParameters().Skip(1).All(p => p.HasDefaultValue || suppliedArgumentValues.Any(s => s.Key == p.Name)))
                .OrderByDescending(m => m.GetParameters().Count(p => suppliedArgumentValues.Any(s => s.Key == p.Name)))
                .FirstOrDefault();
        }

        internal static IList<MethodInfo> FindSinkConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerSinkConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerSinkConfiguration).GetTypeInfo().Assembly))
                found.Add(GetSurrogateConfigurationMethod<LoggerSinkConfiguration, Action<LoggerConfiguration>, LoggingLevelSwitch>((c, a, s) => Logger(c, a, s)));

            return found;
        }

        internal static IList<MethodInfo> FindFilterConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerFilterConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerFilterConfiguration).GetTypeInfo().Assembly))
                found.Add(GetSurrogateConfigurationMethod<LoggerFilterConfiguration, ILogEventFilter, object>((c, f, _) => With(c, f)));

            return found;
        }

        internal static IList<MethodInfo> FindEventEnricherConfigurationMethods(IEnumerable<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationMethods(configurationAssemblies, typeof(LoggerEnrichmentConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerEnrichmentConfiguration).GetTypeInfo().Assembly))
                found.Add(GetSurrogateConfigurationMethod<LoggerEnrichmentConfiguration, object, object>((c, _, __) => FromLogContext(c)));

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
            => loggerFilterConfiguration.With(filter);

        // Unlike the other configuration methods, FromLogContext is an instance method rather than an extension.
        internal static LoggerConfiguration FromLogContext(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
            => loggerEnrichmentConfiguration.FromLogContext();

        // Unlike the other configuration methods, Logger is an instance method rather than an extension.
        internal static LoggerConfiguration Logger(LoggerSinkConfiguration loggerSinkConfiguration, Action<LoggerConfiguration> configureLogger, LoggingLevelSwitch restrictedToMinimumLevel = null)
            => loggerSinkConfiguration.Logger(configureLogger, levelSwitch: restrictedToMinimumLevel);

        internal static MethodInfo GetSurrogateConfigurationMethod<TConfiguration, TArg1, TArg2>(Expression<Action<TConfiguration, TArg1, TArg2>> method)
            => (method.Body as MethodCallExpression)?.Method;
    }
}
