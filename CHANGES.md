# Changelog

3.2.0 (pre-release)

* #162 - LoggingFilterSwitch support
* #202 - added support to AuditTo.Logger
* #203 - added support for custom types in arrays and custom collections
* #218 - fixed an issue with `dotnet restore` with `rid` specified if referenced from `netstandard` project
* #219 - reduced search graph for configuration dlls to avoid native assets
* #221 - added support for conditional/leveled enrichers from Serilog 2.9+
* #222 - updated Microsoft.Extensions.DependencyModel
* #231 - make '$' sign optional for minimum level / filter switch declarations
* #237 - DependencyContextAssemblyFinder fix: check `serilog` at the start of the name for any dependent package
* #239 - handle NotSupportedException for .net 5.0 single file applications
* #260 - skip static constructor on binding for complex parameters types

3.1.0

* #155 - improve SelfLog output when misconfigured
* #160 - respect dynamic logging level changes for LevelSwitch section
* #158 - update NuGet package license format to new format
* #159 - DllScanningAssemblyFinder fixes #157, #150, #122, #156
* #161 - support simple type names for Serilog types
* #151 - no longer rely on static state in ConfigurationReader
* #179 - added missing null checks for settingConfiguration
* #163 - added new ReadFrom.Configuration(...) overloads; marked old as obsolete
* #176 - added test to show how to filter child contexts

3.0.1

* #142 - Fix IConfiguration parameters not being populated
* #143 - Fix ReadFrom.ConfigurationSection() looking for sections below a root Serilog section

3.0.0

* #91 & #92 - Fix cherrypick from master
* #97 - Support of IConfiguration parameters & IConfigurationSection parameters
* #83 - Updated dependencies of Microsoft.Extensions.DependencyModel,
   Microsoft.Extensions.Configuration.Abstraction & Microsoft.Extensions.Options.ConfigurationExtensions per TFM
* #98 - specify string array params
* Target Framework change to netcoreapp2.0
* Build updates including addition of Travis Build
* #105 - detect and fail on ambiguous configurations
* #110 - destructure support
* #111 - case-insensitive argument matching
* #132 - choose string overloads to resolve binding ambiguities
* #134 - specify repository URL in package
* #124 - build a .NET 4.6.1 target
* #136 - control assembly source
* #138 - remove unnecessary package ref
* #139 - remove unused class
* #140 - expand support for destructure/enrich/filter configuration

2.6.1

* #92 - fix WriteTo.Logger handling

2.6.0

* #67 - improve error reporting when trying to convert from a missing class
* #74 - support abstract classes (in addition to interfaces) as values
* #84 - (documentation update)
* #88 - LoggingLevelSwitch support

2.4.0

* #46 - configure sub-loggers through JSON settings
* #48 - permit multiple sinks of the same kind

2.3.1

* #44 - fix ReadFrom.Configuration() on AWS Lambda; VS 2017 tooling

2.3.0

* #40 - fix loading of configuration assemblies with names differing from their packages
* #36 - "Filter" support

2.2.0

* #20 - support MSBuild (non-project.json) projects

2.1.0

* #14 - MinimumLevel.Override()
* #15 - Overload selection fix

2.0.0

* Initial version
