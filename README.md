# Serilog.Settings.Configuration [![Build status](https://ci.appveyor.com/api/projects/status/r2bgfimd9ocr61px/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-settings-configuration/branch/master) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Settings.Configuration.svg?style=flat)](https://www.nuget.org/packages/Serilog.Settings.Configuration/)

A Serilog settings provider that reads from _Microsoft.Extensions.Configuration_ sources, including .NET Core's `appsettings.json` file.

Configuration is read from the `Serilog` section.

```json
{
  "Serilog": {
    "Using":  ["Serilog.Sinks.Console"],
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "%TEMP%\\Logs\\serilog-configuration-sample.txt" } }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
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

This example relies on the _Microsoft.Extensions.Configuration.Json_, _Serilog.Sinks.Console_, _Serilog.Sinks.File_, _Serilog.Enrichers.Environment_, _Serilog.Settings.Configuration_ and _Serilog.Enrichers.Thread_ packages also being installed.

After installing this package, use `ReadFrom.Configuration()` and pass an `IConfiguration` object.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        logger.Information("Hello, world!");
    }
}
```

The `WriteTo` and `Enrich` sections support the same syntax, for example the following is valid if no arguments are needed by the sinks:

```json
"WriteTo": ["Console", "DiagnosticTrace"]
```

Or alternatively, the long-form (`"Name":` ...) syntax from the first example can be used when arguments need to be supplied.

(This package implements a convention using `DependencyContext` to find any package with `Serilog` anywhere in the name and pulls configuration methods from it, so the `Using` example above is redundant.)

### .NET 4.x

To use this package in .NET 4.x applications, add `preserveCompilationContext` to `buildOptions` in _project.json_.

```json
"net4.6": {
   "buildOptions": {
     "preserveCompilationContext": true
   }
},
```

### Level overrides

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

### Environment variables

If your application enables the environment variable configuration source (`AddEnvironmentVariables()`) you can add or override Serilog configuration through the environment.

For example, to set the minimum log level using the _Windows_ command prompt:

```
set Serilog:MinimumLevel=Debug
dotnet run
```

### Nested configuration sections

Some Serilog packages require a reference to a logger configuration object. The sample program in this project illustrates this with the following entry configuring the _Serilog.Sinks.Async_ package to wrap the _Serilog.Sinks.File_ package. The `configure` parameter references the File sink configuration:

```json
"WriteTo:Async": {
  "Name": "Async",
  "Args": {
    "configure": [
      {
        "Name": "File",
        "Args": {
          "path": "%TEMP%\\Logs\\serilog-configuration-sample.txt",
          "outputTemplate":
              "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"
        }
      }
    ]
  }
},
```

### IConfiguration parameter

If a Serilog package requires additional external configuration information (for example, access to a `ConnectionStrings` section, which would be outside of the `Serilog` section), the sink should include an `IConfiguration` parameter in the configuration extension method. This package will automatically populate that parameter. It should not be declared in the argument list in the configuration source.

### Complex parameter value binding

When the configuration specifies a discrete value for a parameter (such as a string literal), the package will attempt to convert that value to the target method's declared CLR type of the parameter. Additional explicit handling is provided for parsing strings to `Uri` and `TimeSpan` objects and `enum` elements.

If the parameter value is not a discrete value, the package will use the configuration binding system provided by _Microsoft.Extensions.Options.ConfigurationExtensions_ to attempt to populate the parameter. Almost anything that can be bound by `IConfiguration.Get<T>` should work with this package. An example of this is the optional `List<Column>` parameter used to configure the .NET Standard version of the _Serilog.Sinks.MSSqlServer_ package.

### IConfigurationSection parameters

Certain Serilog packages may require configuration information that can't be easily represented by discrete values or direct binding-friendly representations. An example might be lists of values to remove from a collection of default values. In this case the method can accept an entire `IConfigurationSection` as a call parameter and this package will recognize that and populate the parameter. In this way, Serilog packages can support arbitrarily complex configuration scenarios.

