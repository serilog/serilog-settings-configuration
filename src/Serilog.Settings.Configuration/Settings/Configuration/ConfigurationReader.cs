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
using System.Text.RegularExpressions;

namespace Serilog.Settings.Configuration
{
    class ConfigurationReader : IConfigurationReader
    {
        const string LevelSwitchNameRegex = @"^\$[A-Za-z]+[A-Za-z0-9]*$";

        static IConfiguration _configuration;

        readonly IConfigurationSection _section;
        readonly DependencyContext _dependencyContext;
        readonly IReadOnlyCollection<Assembly> _configurationAssemblies;

        public ConfigurationReader(IConfiguration configuration, DependencyContext dependencyContext)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _section = configuration.GetSection(ConfigurationLoggerConfigurationExtensions.DefaultSectionName);
            _dependencyContext = dependencyContext;
            _configurationAssemblies = LoadConfigurationAssemblies();
        }

        // Generally the initial call should use IConfiguration rather than IConfigurationSection, otherwise
        // IConfiguration parameters in the target methods will not be populated.
        ConfigurationReader(IConfigurationSection configSection, DependencyContext dependencyContext)
        {
            _section = configSection ?? throw new ArgumentNullException(nameof(configSection));
            _dependencyContext = dependencyContext;
            _configurationAssemblies = LoadConfigurationAssemblies();
        }

        // Used internally for processing nested configuration sections -- see GetMethodCalls below.
        internal ConfigurationReader(IConfigurationSection configSection, IReadOnlyCollection<Assembly> configurationAssemblies, DependencyContext dependencyContext)
        {
            _section = configSection ?? throw new ArgumentNullException(nameof(configSection));
            _dependencyContext = dependencyContext;
            _configurationAssemblies = configurationAssemblies ?? throw new ArgumentNullException(nameof(configurationAssemblies));
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            var declaredLevelSwitches = ProcessLevelSwitchDeclarations();
            ApplyMinimumLevel(loggerConfiguration, declaredLevelSwitches);
            ApplyEnrichment(loggerConfiguration, declaredLevelSwitches);
            ApplyFilters(loggerConfiguration, declaredLevelSwitches);
            ApplyDestructuring(loggerConfiguration, declaredLevelSwitches);
            ApplySinks(loggerConfiguration, declaredLevelSwitches);
            ApplyAuditSinks(loggerConfiguration, declaredLevelSwitches);
        }

        IReadOnlyDictionary<string, LoggingLevelSwitch> ProcessLevelSwitchDeclarations()
        {
            var levelSwitchesDirective = _section.GetSection("LevelSwitches");
            var namedSwitches = new Dictionary<string, LoggingLevelSwitch>();
            foreach (var levelSwitchDeclaration in levelSwitchesDirective.GetChildren())
            {
                var switchName = levelSwitchDeclaration.Key;
                var switchInitialLevel = levelSwitchDeclaration.Value;
                // switchName must be something like $switch to avoid ambiguities
                if (!IsValidSwitchName(switchName))
                {
                    throw new FormatException($"\"{switchName}\" is not a valid name for a Level Switch declaration. Level switch must be declared with a '$' sign, like \"LevelSwitches\" : {{\"$switchName\" : \"InitialLevel\"}}");
                }
                LoggingLevelSwitch newSwitch;
                if (string.IsNullOrEmpty(switchInitialLevel))
                {
                    newSwitch = new LoggingLevelSwitch();
                }
                else
                {
                    var initialLevel = ParseLogEventLevel(switchInitialLevel);
                    newSwitch = new LoggingLevelSwitch(initialLevel);
                }
                namedSwitches.Add(switchName, newSwitch);
            }
            return namedSwitches;
        }

        void ApplyMinimumLevel(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var minimumLevelDirective = _section.GetSection("MinimumLevel");

            var defaultMinLevelDirective = minimumLevelDirective.Value != null ? minimumLevelDirective : minimumLevelDirective.GetSection("Default");
            if (defaultMinLevelDirective.Value != null)
            {
                ApplyMinimumLevel(defaultMinLevelDirective, (configuration, levelSwitch) => configuration.ControlledBy(levelSwitch));
            }

            var minLevelControlledByDirective = minimumLevelDirective.GetSection("ControlledBy");
            if (minLevelControlledByDirective.Value != null)
            {
                var globalMinimumLevelSwitch = declaredLevelSwitches.LookUpSwitchByName(minLevelControlledByDirective.Value);
                // not calling ApplyMinimumLevel local function because here we have a reference to a LogLevelSwitch already
                loggerConfiguration.MinimumLevel.ControlledBy(globalMinimumLevelSwitch);
            }

            foreach (var overrideDirective in minimumLevelDirective.GetSection("Override").GetChildren())
            {
                var overridePrefix = overrideDirective.Key;
                var overridenLevelOrSwitch = overrideDirective.Value;
                if (Enum.TryParse(overridenLevelOrSwitch, out LogEventLevel _))
                {
                    ApplyMinimumLevel(overrideDirective, (configuration, levelSwitch) => configuration.Override(overridePrefix, levelSwitch));
                }
                else
                {
                    var overrideSwitch = declaredLevelSwitches.LookUpSwitchByName(overridenLevelOrSwitch);
                    // not calling ApplyMinimumLevel local function because here we have a reference to a LogLevelSwitch already
                    loggerConfiguration.MinimumLevel.Override(overridePrefix, overrideSwitch);
                }
            }

            void ApplyMinimumLevel(IConfigurationSection directive, Action<LoggerMinimumLevelConfiguration, LoggingLevelSwitch> applyConfigAction)
            {
                var minimumLevel = ParseLogEventLevel(directive.Value);

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

        void ApplyFilters(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var filterDirective = _section.GetSection("Filter");
            if (filterDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(filterDirective);
                CallConfigurationMethods(methodCalls, FindFilterConfigurationMethods(_configurationAssemblies), loggerConfiguration.Filter, declaredLevelSwitches);
            }
        }

        void ApplyDestructuring(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var destructureDirective = _section.GetSection("Destructure");
            if (destructureDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(destructureDirective);
                CallConfigurationMethods(methodCalls, FindDestructureConfigurationMethods(_configurationAssemblies), loggerConfiguration.Destructure, declaredLevelSwitches);
            }
        }

        void ApplySinks(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var writeToDirective = _section.GetSection("WriteTo");
            if (writeToDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(writeToDirective);
                CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerConfiguration.WriteTo, declaredLevelSwitches);
            }
        }

        void ApplyAuditSinks(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var auditToDirective = _section.GetSection("AuditTo");
            if (auditToDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(auditToDirective);
                CallConfigurationMethods(methodCalls, FindAuditSinkConfigurationMethods(_configurationAssemblies), loggerConfiguration.AuditTo, declaredLevelSwitches);
            }
        }

        void IConfigurationReader.ApplySinks(LoggerSinkConfiguration loggerSinkConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var methodCalls = GetMethodCalls(_section);
            CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerSinkConfiguration, declaredLevelSwitches);
        }

        void ApplyEnrichment(LoggerConfiguration loggerConfiguration, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            var enrichDirective = _section.GetSection("Enrich");
            if (enrichDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(enrichDirective);
                CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(_configurationAssemblies), loggerConfiguration.Enrich, declaredLevelSwitches);
            }

            var propertiesDirective = _section.GetSection("Properties");
            if (propertiesDirective.GetChildren().Any())
            {
                foreach (var enrichProperyDirective in propertiesDirective.GetChildren())
                {
                    loggerConfiguration.Enrich.WithProperty(enrichProperyDirective.Key, enrichProperyDirective.Value);
                }
            }
        }

        internal ILookup<string, Dictionary<string, IConfigurationArgumentValue>> GetMethodCalls(IConfigurationSection directive)
        {
            var children = directive.GetChildren().ToList();

            var result =
                (from child in children
                 where child.Value != null // Plain string
                 select new { Name = child.Value, Args = new Dictionary<string, IConfigurationArgumentValue>() })
                     .Concat(
                (from child in children
                 where child.Value == null
                 let name = GetSectionName(child)
                 let callArgs = (from argument in child.GetSection("Args").GetChildren()
                                 select new
                                 {
                                     Name = argument.Key,
                                     Value = GetArgumentValue(argument)
                                 }).ToDictionary(p => p.Name, p => p.Value)
                 select new { Name = name, Args = callArgs }))
                     .ToLookup(p => p.Name, p => p.Args);

            return result;

            IConfigurationArgumentValue GetArgumentValue(IConfigurationSection argumentSection)
            {
                IConfigurationArgumentValue argumentValue;

                // Reject configurations where an element has both scalar and complex
                // values as a result of reading multiple configuration sources.
                if (argumentSection.Value != null && argumentSection.GetChildren().Any())
                    throw new InvalidOperationException(
                        $"The value for the argument '{argumentSection.Path}' is assigned different value " +
                        "types in more than one configuration source. Ensure all configurations consistently " +
                        "use either a scalar (int, string, boolean) or a complex (array, section, list, " +
                        "POCO, etc.) type for this argument value.");

                if (argumentSection.Value != null)
                {
                    argumentValue = new StringArgumentValue(() => argumentSection.Value, argumentSection.GetReloadToken);
                }
                else
                {
                    argumentValue = new ObjectArgumentValue(argumentSection, _configurationAssemblies, _dependencyContext);
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

        IReadOnlyCollection<Assembly> LoadConfigurationAssemblies()
        {
            var assemblies = new Dictionary<string, Assembly>();

            var usingSection = _section.GetSection("Using");
            if (usingSection.GetChildren().Any())
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

            return assemblies.Values.ToList().AsReadOnly();
        }

        AssemblyName[] GetSerilogConfigurationAssemblies()
        {
            // ReSharper disable once RedundantAssignment
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
                query = from outputAssemblyPath in System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                        let assemblyFileName = System.IO.Path.GetFileNameWithoutExtension(outputAssemblyPath)
                        where filter(assemblyFileName)
                        select AssemblyName.GetAssemblyName(outputAssemblyPath);
            }

            return query.ToArray();
        }

        static void CallConfigurationMethods(ILookup<string, Dictionary<string, IConfigurationArgumentValue>> methods, IList<MethodInfo> configurationMethods, object receiver, IReadOnlyDictionary<string, LoggingLevelSwitch> declaredLevelSwitches)
        {
            foreach (var method in methods.SelectMany(g => g.Select(x => new { g.Key, Value = x })))
            {
                var methodInfo = SelectConfigurationMethod(configurationMethods, method.Key, method.Value);

                if (methodInfo != null)
                {
                    var call = (from p in methodInfo.GetParameters().Skip(1)
                                let directive = method.Value.FirstOrDefault(s => s.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                                select directive.Key == null ? p.DefaultValue : directive.Value.ConvertTo(p.ParameterType, declaredLevelSwitches)).ToList();

                    var parm = methodInfo.GetParameters().FirstOrDefault(i => i.ParameterType == typeof(IConfiguration));
                    if (parm != null) call[parm.Position - 1] = _configuration;

                    call.Insert(0, receiver);

                    methodInfo.Invoke(null, call.ToArray());
                }
            }
        }

        internal static MethodInfo SelectConfigurationMethod(IEnumerable<MethodInfo> candidateMethods, string name, Dictionary<string, IConfigurationArgumentValue> suppliedArgumentValues)
        {
            // Per issue #111, it is safe to use case-insensitive matching on argument names. The CLR doesn't permit this type
            // of overloading, and the Microsoft.Extensions.Configuration keys are case-insensitive (case is preserved with some
            // config sources, but key-matching is case-insensitive and case-preservation does not appear to be guaranteed).
            return candidateMethods
                .Where(m => m.Name == name &&
                            m.GetParameters().Skip(1)
                            .All(p => p.HasDefaultValue || suppliedArgumentValues.Any(s => s.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))))
                .OrderByDescending(m =>
                {
                    var matchingArgs = m.GetParameters().Where(p => suppliedArgumentValues.Any(s => s.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                    // Prefer the configuration method with most number of matching arguments and of those the ones with
                    // the most string type parameters to predict best match with least type casting
                    return new Tuple<int, int>(
                        matchingArgs.Count,
                        matchingArgs.Count(p => p.ParameterType == typeof(string)));
                })
                .FirstOrDefault();
        }

        static IList<MethodInfo> FindSinkConfigurationMethods(IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationExtensionMethods(configurationAssemblies, typeof(LoggerSinkConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerSinkConfiguration).GetTypeInfo().Assembly))
                found.AddRange(SurrogateConfigurationMethods.WriteTo);

            return found;
        }

        static IList<MethodInfo> FindAuditSinkConfigurationMethods(IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationExtensionMethods(configurationAssemblies, typeof(LoggerAuditSinkConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerAuditSinkConfiguration).GetTypeInfo().Assembly))
                found.AddRange(SurrogateConfigurationMethods.AuditTo);
            return found;
        }

        static IList<MethodInfo> FindFilterConfigurationMethods(IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationExtensionMethods(configurationAssemblies, typeof(LoggerFilterConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerFilterConfiguration).GetTypeInfo().Assembly))
                found.AddRange(SurrogateConfigurationMethods.Filter);

            return found;
        }

        static IList<MethodInfo> FindDestructureConfigurationMethods(IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationExtensionMethods(configurationAssemblies, typeof(LoggerDestructuringConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerDestructuringConfiguration).GetTypeInfo().Assembly))
                found.AddRange(SurrogateConfigurationMethods.Destructure);

            return found;
        }

        static IList<MethodInfo> FindEventEnricherConfigurationMethods(IReadOnlyCollection<Assembly> configurationAssemblies)
        {
            var found = FindConfigurationExtensionMethods(configurationAssemblies, typeof(LoggerEnrichmentConfiguration));
            if (configurationAssemblies.Contains(typeof(LoggerEnrichmentConfiguration).GetTypeInfo().Assembly))
                found.AddRange(SurrogateConfigurationMethods.Enrich);

            return found;
        }

        static List<MethodInfo> FindConfigurationExtensionMethods(IReadOnlyCollection<Assembly> configurationAssemblies, Type configType)
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

        internal static bool IsValidSwitchName(string input)
        {
            return Regex.IsMatch(input, LevelSwitchNameRegex);
        }

        static LogEventLevel ParseLogEventLevel(string value)
        {
            if (!Enum.TryParse(value, out LogEventLevel parsedLevel))
                throw new InvalidOperationException($"The value {value} is not a valid Serilog level.");
            return parsedLevel;
        }

    }
}
