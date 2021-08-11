using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Settings.Configuration.Assemblies;

namespace Serilog.Settings.Configuration
{
    class ConfigurationReader : IConfigurationReader
    {
        const string LevelSwitchNameRegex = @"^\${0,1}[A-Za-z]+[A-Za-z0-9]*$";

        readonly IConfigurationSection _section;
        readonly IReadOnlyCollection<Assembly> _configurationAssemblies;
        readonly ResolutionContext _resolutionContext;

        public ConfigurationReader(IConfigurationSection configSection, AssemblyFinder assemblyFinder, IConfiguration configuration = null)
        {
            _section = configSection ?? throw new ArgumentNullException(nameof(configSection));
            _configurationAssemblies = LoadConfigurationAssemblies(_section, assemblyFinder);
            _resolutionContext = new ResolutionContext(configuration);
        }

        // Used internally for processing nested configuration sections -- see GetMethodCalls below.
        internal ConfigurationReader(IConfigurationSection configSection, IReadOnlyCollection<Assembly> configurationAssemblies, ResolutionContext resolutionContext)
        {
            _section = configSection ?? throw new ArgumentNullException(nameof(configSection));
            _configurationAssemblies = configurationAssemblies ?? throw new ArgumentNullException(nameof(configurationAssemblies));
            _resolutionContext = resolutionContext ?? throw new ArgumentNullException(nameof(resolutionContext));
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            ProcessLevelSwitchDeclarations();
            ProcessFilterSwitchDeclarations();

            ApplyMinimumLevel(loggerConfiguration);
            ApplyEnrichment(loggerConfiguration);
            ApplyFilters(loggerConfiguration);
            ApplyDestructuring(loggerConfiguration);
            ApplySinks(loggerConfiguration);
            ApplyAuditSinks(loggerConfiguration);
        }

        void ProcessFilterSwitchDeclarations()
        {
            var filterSwitchesDirective = _section.GetSection("FilterSwitches");

            foreach (var filterSwitchDeclaration in filterSwitchesDirective.GetChildren())
            {
                var filterSwitch = LoggingFilterSwitchProxy.Create();
                if (filterSwitch == null)
                {
                    SelfLog.WriteLine($"FilterSwitches section found, but neither Serilog.Expressions nor Serilog.Filters.Expressions is referenced.");
                    break;
                }

                var switchName = filterSwitchDeclaration.Key;
                // switchName must be something like $switch to avoid ambiguities
                if (!IsValidSwitchName(switchName))
                {
                    throw new FormatException($"\"{switchName}\" is not a valid name for a Filter Switch declaration. The first character of the name must be a letter or '$' sign, like \"FilterSwitches\" : {{\"$switchName\" : \"{{FilterExpression}}\"}}");
                }

                SetFilterSwitch(throwOnError: true);
                SubscribeToFilterExpressionChanges();

                _resolutionContext.AddFilterSwitch(switchName, filterSwitch);

                void SubscribeToFilterExpressionChanges()
                {
                    ChangeToken.OnChange(filterSwitchDeclaration.GetReloadToken, () => SetFilterSwitch(throwOnError: false));
                }

                void SetFilterSwitch(bool throwOnError)
                {
                    var filterExpr = filterSwitchDeclaration.Value;
                    if (string.IsNullOrWhiteSpace(filterExpr))
                    {
                        filterSwitch.Expression = null;
                        return;
                    }

                    try
                    {
                        filterSwitch.Expression = filterExpr;
                    }
                    catch (Exception e)
                    {
                        var errMsg = $"The expression '{filterExpr}' is invalid filter expression: {e.Message}.";
                        if (throwOnError)
                        {
                            throw new InvalidOperationException(errMsg, e);
                        }

                        SelfLog.WriteLine(errMsg);
                    }
                }
            }
        }

        void ProcessLevelSwitchDeclarations()
        {
            var levelSwitchesDirective = _section.GetSection("LevelSwitches");
            foreach (var levelSwitchDeclaration in levelSwitchesDirective.GetChildren())
            {
                var switchName = levelSwitchDeclaration.Key;
                var switchInitialLevel = levelSwitchDeclaration.Value;
                // switchName must be something like $switch to avoid ambiguities
                if (!IsValidSwitchName(switchName))
                {
                    throw new FormatException($"\"{switchName}\" is not a valid name for a Level Switch declaration. The first character of the name must be a letter or '$' sign, like \"LevelSwitches\" : {{\"$switchName\" : \"InitialLevel\"}}");
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

                SubscribeToLoggingLevelChanges(levelSwitchDeclaration, newSwitch);

                // make them available later on when resolving argument values
                _resolutionContext.AddLevelSwitch(switchName, newSwitch);
            }
        }

        void ApplyMinimumLevel(LoggerConfiguration loggerConfiguration)
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
                var globalMinimumLevelSwitch = _resolutionContext.LookUpLevelSwitchByName(minLevelControlledByDirective.Value);
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
                    var overrideSwitch = _resolutionContext.LookUpLevelSwitchByName(overridenLevelOrSwitch);
                    // not calling ApplyMinimumLevel local function because here we have a reference to a LogLevelSwitch already
                    loggerConfiguration.MinimumLevel.Override(overridePrefix, overrideSwitch);
                }
            }

            void ApplyMinimumLevel(IConfigurationSection directive, Action<LoggerMinimumLevelConfiguration, LoggingLevelSwitch> applyConfigAction)
            {
                var minimumLevel = ParseLogEventLevel(directive.Value);

                var levelSwitch = new LoggingLevelSwitch(minimumLevel);
                applyConfigAction(loggerConfiguration.MinimumLevel, levelSwitch);

                SubscribeToLoggingLevelChanges(directive, levelSwitch);
            }
        }

        void SubscribeToLoggingLevelChanges(IConfigurationSection levelSection, LoggingLevelSwitch levelSwitch)
        {
            ChangeToken.OnChange(
                levelSection.GetReloadToken,
                () =>
                {
                    if (Enum.TryParse(levelSection.Value, out LogEventLevel minimumLevel))
                        levelSwitch.MinimumLevel = minimumLevel;
                    else
                        SelfLog.WriteLine($"The value {levelSection.Value} is not a valid Serilog level.");
                });
        }

        void ApplyFilters(LoggerConfiguration loggerConfiguration)
        {
            var filterDirective = _section.GetSection("Filter");
            if (filterDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(filterDirective);
                CallConfigurationMethods(methodCalls, FindFilterConfigurationMethods(_configurationAssemblies), loggerConfiguration.Filter);
            }
        }

        void ApplyDestructuring(LoggerConfiguration loggerConfiguration)
        {
            var destructureDirective = _section.GetSection("Destructure");
            if (destructureDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(destructureDirective);
                CallConfigurationMethods(methodCalls, FindDestructureConfigurationMethods(_configurationAssemblies), loggerConfiguration.Destructure);
            }
        }

        void ApplySinks(LoggerConfiguration loggerConfiguration)
        {
            var writeToDirective = _section.GetSection("WriteTo");
            if (writeToDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(writeToDirective);
                CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerConfiguration.WriteTo);
            }
        }

        void ApplyAuditSinks(LoggerConfiguration loggerConfiguration)
        {
            var auditToDirective = _section.GetSection("AuditTo");
            if (auditToDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(auditToDirective);
                CallConfigurationMethods(methodCalls, FindAuditSinkConfigurationMethods(_configurationAssemblies), loggerConfiguration.AuditTo);
            }
        }

        void IConfigurationReader.ApplySinks(LoggerSinkConfiguration loggerSinkConfiguration)
        {
            var methodCalls = GetMethodCalls(_section);
            CallConfigurationMethods(methodCalls, FindSinkConfigurationMethods(_configurationAssemblies), loggerSinkConfiguration);
        }

        void IConfigurationReader.ApplyEnrichment(LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
        {
            var methodCalls = GetMethodCalls(_section);
            CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(_configurationAssemblies), loggerEnrichmentConfiguration);
        }

        void ApplyEnrichment(LoggerConfiguration loggerConfiguration)
        {
            var enrichDirective = _section.GetSection("Enrich");
            if (enrichDirective.GetChildren().Any())
            {
                var methodCalls = GetMethodCalls(enrichDirective);
                CallConfigurationMethods(methodCalls, FindEventEnricherConfigurationMethods(_configurationAssemblies), loggerConfiguration.Enrich);
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
                                     Value = GetArgumentValue(argument, _configurationAssemblies)
                                 }).ToDictionary(p => p.Name, p => p.Value)
                 select new { Name = name, Args = callArgs }))
                     .ToLookup(p => p.Name, p => p.Args);

            return result;

            static string GetSectionName(IConfigurationSection s)
            {
                var name = s.GetSection("Name");
                if (name.Value == null)
                    throw new InvalidOperationException($"The configuration value in {name.Path} has no 'Name' element.");

                return name.Value;
            }
        }

        internal static IConfigurationArgumentValue GetArgumentValue(IConfigurationSection argumentSection, IReadOnlyCollection<Assembly> configurationAssemblies)
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
                argumentValue = new StringArgumentValue(argumentSection.Value);
            }
            else
            {
                argumentValue = new ObjectArgumentValue(argumentSection, configurationAssemblies);
            }

            return argumentValue;
        }

        static IReadOnlyCollection<Assembly> LoadConfigurationAssemblies(IConfigurationSection section, AssemblyFinder assemblyFinder)
        {
            var serilogAssembly = typeof(ILogger).Assembly;
            var assemblies = new Dictionary<string, Assembly> { [serilogAssembly.FullName] = serilogAssembly };

            var usingSection = section.GetSection("Using");
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

            foreach (var assemblyName in assemblyFinder.FindAssembliesContainingName("serilog"))
            {
                var assumed = Assembly.Load(assemblyName);
                if (assumed != null && !assemblies.ContainsKey(assumed.FullName))
                    assemblies.Add(assumed.FullName, assumed);
            }

            return assemblies.Values.ToList().AsReadOnly();
        }

        void CallConfigurationMethods(ILookup<string, Dictionary<string, IConfigurationArgumentValue>> methods, IList<MethodInfo> configurationMethods, object receiver)
        {
            foreach (var method in methods.SelectMany(g => g.Select(x => new { g.Key, Value = x })))
            {
                var methodInfo = SelectConfigurationMethod(configurationMethods, method.Key, method.Value.Keys);

                if (methodInfo != null)
                {
                    var call = (from p in methodInfo.GetParameters().Skip(1)
                                let directive = method.Value.FirstOrDefault(s => ParameterNameMatches(p.Name, s.Key))
                                select directive.Key == null
                                    ? GetImplicitValueForNotSpecifiedKey(p, methodInfo)
                                    : directive.Value.ConvertTo(p.ParameterType, _resolutionContext)).ToList();

                    call.Insert(0, receiver);
                    methodInfo.Invoke(null, call.ToArray());
                }
            }
        }

        static bool HasImplicitValueWhenNotSpecified(ParameterInfo paramInfo)
        {
            return paramInfo.HasDefaultValue
               // parameters of type IConfiguration are implicitly populated with provided Configuration
               || paramInfo.ParameterType == typeof(IConfiguration);
        }

        object GetImplicitValueForNotSpecifiedKey(ParameterInfo parameter, MethodInfo methodToInvoke)
        {
            if (!HasImplicitValueWhenNotSpecified(parameter))
            {
                throw new InvalidOperationException("GetImplicitValueForNotSpecifiedKey() should only be called for parameters for which HasImplicitValueWhenNotSpecified() is true. " +
                                                    "This means something is wrong in the Serilog.Settings.Configuration code.");
            }

            if (parameter.ParameterType == typeof(IConfiguration))
            {
                if (_resolutionContext.HasAppConfiguration)
                {
                    return _resolutionContext.AppConfiguration;
                }
                if (parameter.HasDefaultValue)
                {
                    return parameter.DefaultValue;
                }

                throw new InvalidOperationException("Trying to invoke a configuration method accepting a `IConfiguration` argument. " +
                                                              $"This is not supported when only a `IConfigSection` has been provided. (method '{methodToInvoke}')");
            }

            return parameter.DefaultValue;
        }

        internal static MethodInfo SelectConfigurationMethod(IEnumerable<MethodInfo> candidateMethods, string name, IEnumerable<string> suppliedArgumentNames)
        {
            // Per issue #111, it is safe to use case-insensitive matching on argument names. The CLR doesn't permit this type
            // of overloading, and the Microsoft.Extensions.Configuration keys are case-insensitive (case is preserved with some
            // config sources, but key-matching is case-insensitive and case-preservation does not appear to be guaranteed).
            var selectedMethod = candidateMethods
                .Where(m => m.Name == name)
                .Where(m => m.GetParameters()
                            .Skip(1)
                            .All(p => HasImplicitValueWhenNotSpecified(p) ||
                                      ParameterNameMatches(p.Name, suppliedArgumentNames)))
                .OrderByDescending(m =>
                {
                    var matchingArgs = m.GetParameters().Where(p => ParameterNameMatches(p.Name, suppliedArgumentNames)).ToList();

                    // Prefer the configuration method with most number of matching arguments and of those the ones with
                    // the most string type parameters to predict best match with least type casting
                    return new Tuple<int, int>(
                        matchingArgs.Count,
                        matchingArgs.Count(p => p.ParameterType == typeof(string)));
                })
                .FirstOrDefault();

            if (selectedMethod == null)
            {
                var methodsByName = candidateMethods
                    .Where(m => m.Name == name)
                    .Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Skip(1).Select(p => p.Name))})")
                    .ToList();

                if (!methodsByName.Any())
                    SelfLog.WriteLine($"Unable to find a method called {name}. Candidate methods are:{Environment.NewLine}{string.Join(Environment.NewLine, candidateMethods)}");
                else
                    SelfLog.WriteLine($"Unable to find a method called {name} "
                    + (suppliedArgumentNames.Any()
                        ? "for supplied arguments: " + string.Join(", ", suppliedArgumentNames)
                        : "with no supplied arguments")
                    + ". Candidate methods are:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, methodsByName));
            }

            return selectedMethod;
        }

        static bool ParameterNameMatches(string actualParameterName, string suppliedName)
        {
            return suppliedName.Equals(actualParameterName, StringComparison.OrdinalIgnoreCase);
        }

        static bool ParameterNameMatches(string actualParameterName, IEnumerable<string> suppliedNames)
        {
            return suppliedNames.Any(s => ParameterNameMatches(actualParameterName, s));
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
