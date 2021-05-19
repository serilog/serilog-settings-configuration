# Serilog.Settings.Configuration [![Build status](https://ci.appveyor.com/api/projects/status/r2bgfimd9ocr61px/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-settings-configuration/branch/master) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Settings.Configuration.svg?style=flat)](https://www.nuget.org/packages/Serilog.Settings.Configuration/)

A Serilog settings provider that reads from [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1) sources, including .NET Core's `appsettings.json` file.

By default, configuration is read from the `Serilog` section.

```json
{
  "Serilog": {
    "Using":  [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "Logs/log.txt" } }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Destructure": [
      { "Name": "With", "Args": { "policy": "Sample.CustomPolicy, Sample" } },
      { "Name": "ToMaximumDepth", "Args": { "maximumDestructuringDepth": 4 } },
      { "Name": "ToMaximumStringLength", "Args": { "maximumStringLength": 100 } },
      { "Name": "ToMaximumCollectionCount", "Args": { "maximumCollectionCount": 10 } }
    ],
    "Properties": {
        "Application": "Sample"
    }
  }
}
```

After installing this package, use `ReadFrom.Configuration()` and pass an `IConfiguration` object.

```csharp
static void Main(string[] args)
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build();

    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

    logger.Information("Hello, world!");
}
```

This example relies on the _[Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/)_, _[Serilog.Sinks.Console](https://github.com/serilog/serilog-sinks-console)_, _[Serilog.Sinks.File](https://github.com/serilog/serilog-sinks-file)_, _[Serilog.Enrichers.Environment](https://github.com/serilog/serilog-enrichers-environment)_ and _[Serilog.Enrichers.Thread](https://github.com/serilog/serilog-enrichers-thread)_ packages also being installed.

For a more sophisticated example go to the [sample](sample/Sample) folder.

## Syntax description

### Root section name

Root section name can be changed:

```json
{
  "CustomSection": {
    ...
  }
}
```

```csharp
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration, sectionName: "CustomSection")
    .CreateLogger();
```

### Using section and auto-discovery of configuration assemblies

`Using` section contains list of **assemblies** in which configuration methods (`WriteTo.File()`, `Enrich.WithThreadId()`) reside.

```json
"Serilog": {
    "Using":  [ "Serilog.Sinks.Console", "Serilog.Enrichers.Thread", /* ... */ ],
    // ...
}
```

For .NET Core projects build tools produce `.deps.json` files and this package implements a convention using `Microsoft.Extensions.DependencyModel` to find any package among dependencies with `Serilog` anywhere in the name and pulls configuration methods from it, so the `Using` section in example above can be omitted:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [ "Console" ],
    ...
  }
}
```

In order to utilize this convention for .NET Framework projects which are built with .NET Core CLI tools specify `PreserveCompilationContext` to `true` in the csproj properties:

```xml
<PropertyGroup Condition=" '$(TargetFramework)' == 'net46' ">
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

In case of [non-standard](#azure-functions-v2-v3) dependency management you can pass a custom `DependencyContext` object:

```csharp
var functionDependencyContext = DependencyContext.Load(typeof(Startup).Assembly);

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(hostConfig, sectionName: "AzureFunctionsJobHost:Serilog", dependencyContext: functionDependencyContext)
    .CreateLogger();
```

For legacy .NET Framework projects it also scans default probing path(s).

For all other cases, as well as in the case of non-conventional configuration assembly names **DO** use [Using](#using-section-and-auto-discovery-of-configuration-assemblies) section.

#### .NET 5.0 Single File Applications

Currently, auto-discovery of configuration assemblies is not supported in bundled mode. **DO** use [Using](#using-section-and-auto-discovery-of-configuration-assemblies) section for workaround.

### MinimumLevel, LevelSwitches, overrides and dynamic reload

The `MinimumLevel` configuration property can be set to a single value as in the sample above, or, levels can be overridden per logging source.

This is useful in ASP.NET Core applications, which will often specify minimum level as:

```json
"MinimumLevel": {
    "Default": "Information",
    "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
    }
}
```

`MinimumLevel` section also respects dynamic reload if the underlying provider supports it.

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(path: "appsettings.json", reloadOnChange: true)
    .Build();
```

Any changes for `Default`, `Microsoft`, `System` sources will be applied at runtime.

(Note: only existing sources are respected for a dynamic update. Inserting new records in `Override` section is **not** supported.)

You can also declare `LoggingLevelSwitch`-es in custom section and reference them for sink parameters:

```json
{
    "Serilog": {
        "LevelSwitches": { "controlSwitch": "Verbose" },
        "WriteTo": [
            {
                "Name": "Seq",
                "Args": {
                    "serverUrl": "http://localhost:5341",
                    "apiKey": "yeEZyL3SMcxEKUijBjN",
                    "controlLevelSwitch": "$controlSwitch"
                }
            }
        ]
    }
}
```

Level updates to switches are also respected for a dynamic update.

### WriteTo, Enrich, AuditTo, Destructure sections

These sections support simplified syntax, for example the following is valid if no arguments are needed by the sinks:

```json
"WriteTo": [ "Console", "DiagnosticTrace" ]
```

Or alternatively, the long-form (`"Name":` ...) syntax from the example above can be used when arguments need to be supplied.

By `Microsoft.Extensions.Configuration.Json` convention, array syntax implicitly defines index for each element in order to make unique paths for configuration keys. So the example above is equivalent to:

```json
"WriteTo": {
    "0": "Console",
    "1": "DiagnosticTrace"
}
```

And

```json
"WriteTo:0": "Console",
"WriteTo:1": "DiagnosticTrace"
```

(The result paths for the keys will be the same, i.e. `Serilog:WriteTo:0` and `Serilog:WriteTo:1`)

When overriding settings with [environment variables](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#environment-variables) it becomes less convenient and fragile, so you can specify custom names:

```json
"WriteTo": {
    "ConsoleSink": "Console",
    "DiagnosticTraceSink": { "Name": "DiagnosticTrace" }
}
```

### Properties section

This section defines a static list of key-value pairs that will enrich log events.

### Filter section

This section defines filters that will be applied to log events. It is especially usefull in combination with _[Serilog.Expressions](https://github.com/serilog/serilog-expressions)_ (or legacy _[Serilog.Filters.Expressions](https://github.com/serilog/serilog-filters-expressions)_) package so you can write expression in text form:

```json
"Filter": [{
  "Name": "ByIncludingOnly",
  "Args": {
      "expression": "Application = 'Sample'"
  }
}]
```

Using this package you can also declare `LoggingFilterSwitch`-es in custom section and reference them for filter parameters:

```json
{
    "Serilog": {
        "FilterSwitches": { "filterSwitch": "Application = 'Sample'" },
        "Filter": [
            {
                "Name": "ControlledBy",
                "Args": {
                    "switch": "$filterSwitch"
                }
            }
        ]
}
```

Level updates to switches are also respected for a dynamic update.

### Nested configuration sections

Some Serilog packages require a reference to a logger configuration object. The sample program in this project illustrates this with the following entry configuring the _[Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async)_ package to wrap the _[Serilog.Sinks.File](https://github.com/serilog/serilog-sinks-file)_ package. The `configure` parameter references the File sink configuration:

```json
"WriteTo:Async": {
  "Name": "Async",
  "Args": {
    "configure": [
      {
        "Name": "File",
        "Args": {
          "path": "%TEMP%/Logs/serilog-configuration-sample.txt",
          "outputTemplate":
              "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"
        }
      }
    ]
  }
},
```

## Arguments binding

When the configuration specifies a discrete value for a parameter (such as a string literal), the package will attempt to convert that value to the target method's declared CLR type of the parameter. Additional explicit handling is provided for parsing strings to `Uri`, `TimeSpan`, `enum`, arrays and custom collections.

### Complex parameter value binding

If the parameter value is not a discrete value, the package will use the configuration binding system provided by _[Microsoft.Extensions.Options.ConfigurationExtensions](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/)_ to attempt to populate the parameter. Almost anything that can be bound by `IConfiguration.Get<T>` should work with this package. An example of this is the optional `List<Column>` parameter used to configure the .NET Standard version of the _[Serilog.Sinks.MSSqlServer](https://github.com/serilog/serilog-sinks-mssqlserver)_ package.

### Abstract parameter types

If parameter type is an interface or an abstract class you need to specify the full type name that implements abstract type. The implementation type should have parameterless constructor.

```json
"Destructure": [
    { "Name": "With", "Args": { "policy": "Sample.CustomPolicy, Sample" } },
    ...
],
```

### IConfiguration parameter

If a Serilog package requires additional external configuration information (for example, access to a `ConnectionStrings` section, which would be outside of the `Serilog` section), the sink should include an `IConfiguration` parameter in the configuration extension method. This package will automatically populate that parameter. It should not be declared in the argument list in the configuration source.

### IConfigurationSection parameters

Certain Serilog packages may require configuration information that can't be easily represented by discrete values or direct binding-friendly representations. An example might be lists of values to remove from a collection of default values. In this case the method can accept an entire `IConfigurationSection` as a call parameter and this package will recognize that and populate the parameter. In this way, Serilog packages can support arbitrarily complex configuration scenarios.

## Samples

### Azure Functions (v2, v3)

hosts.json

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingExcludedTypes": "Request",
      "samplingSettings": {
        "isEnabled": true
      }
    }
  },
  "Serilog": {
    "MinimumLevel": {
        "Default": "Information",
        "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
        }
    },
    "Enrich": [ "FromLogContext" ],
    "WriteTo": [
      { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
    ]
  }
}
```

In `Startup.cs` section name should be prefixed with [AzureFunctionsJobHost](https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#azurefunctionsjobhost__)

```csharp
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider>(sp =>
        {
            var functionDependencyContext = DependencyContext.Load(typeof(Startup).Assembly);

            var hostConfig = sp.GetRequiredService<IConfiguration>();
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostConfig, sectionName: "AzureFunctionsJobHost:Serilog", dependencyContext: functionDependencyContext)
                .CreateLogger();

            return new SerilogLoggerProvider(logger, dispose: true);
        });
    }
}
```

In order to make auto-discovery of configuration assemblies work, modify Function's csproj file

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- ... -->

  <!-- add this targets -->
  <Target Name="FunctionsPostBuildDepsCopy" AfterTargets="PostBuildEvent">
    <Copy SourceFiles="$(OutDir)$(AssemblyName).deps.json" DestinationFiles="$(OutDir)bin\$(AssemblyName).deps.json" />
  </Target>

  <Target Name="FunctionsPublishDepsCopy" AfterTargets="Publish">
    <Copy SourceFiles="$(PublishDir)$(AssemblyName).deps.json" DestinationFiles="$(PublishDir)bin\$(AssemblyName).deps.json" />
  </Target>

</Project>
```
