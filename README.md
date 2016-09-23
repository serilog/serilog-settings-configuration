# Serilog.Settings.Configuration [![Build status](https://ci.appveyor.com/api/projects/status/r2bgfimd9ocr61px/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-settings-configuration/branch/master)

A Serilog settings provider that reads from [_Microsoft.Extensions.Configuration_](https://github.com/aspnet/Configuration).

Configuration is read from the `Serilog` section.

### json (_Microsoft.Extensions.Configuration.Json_ package)
```json
{
  "Serilog": {
    "Using":  ["Serilog.Sinks.Literate"],
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "LiterateConsole" },
      { "Name": "File", "Args": { "path": "%TEMP%\\Logs\\serilog-configuration-sample.txt" } }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
		"Application": "Sample"
    }
  }
}
```
```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Literate" ],
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "MyApp.Something.Tricky": "Verbose"
      }
    },
    "WriteTo:LiterateConsole": {
      "Name": "LiterateConsole",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {SourceContext} [{Level}] {Message}{NewLine}{Exception}"
      }
    },
    "WriteTo:File1": {
      "Name": "File",
      "Args": {
        "path": "%TEMP%\\Logs\\serilog-configuration-sample-1.txt",
        "outputTemplate": "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"
      }
    },
    "WriteTo:File2": {
      "Name": "File",
      "Args": {
        "path": "%TEMP%\\Logs\\serilog-configuration-sample-2.txt",
        "outputTemplate": "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"
      }
    },
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "Sample"
    }
  }
}
```

### ini (_Microsoft.Extensions.Configuration.Ini_ package)
```ini
[Serilog:Using]
Literate="Serilog.Sinks.Literate"

[Serilog:MinimumLevel]
Default=Debug
Override:Microsoft=Warning
Override:MyApp.Something.Tricky=Verbose

[Serilog:WriteTo:LiterateConsole]
Name=LiterateConsole
Args:outputTemplate="[{Timestamp:HH:mm:ss} {SourceContext} [{Level}] {Message}{NewLine}{Exception}"

[Serilog:WriteTo:File1]
Name=File
Args:path="%TEMP%\Logs\serilog-configuration-sample-1.txt"
Args:outputTemplate="{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"

[Serilog:WriteTo:File2]
Name=File
Args:path="%TEMP%\Logs\serilog-configuration-sample-2.txt"
Args:outputTemplate="{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"

[Serilog:Enrich]
0=FromLogContext
1=WithMachineName
2=WithThreadId

[Serilog:Properties]
Application=Sample
```

### xml (_Microsoft.Extensions.Configuration.Xml_ package)
```xml
<settings>
  <serilog>
    <using name="Literate">Serilog.Sinks.Literate</using>
    <minimumLevel default="Debug">
      <override>
        <Microsoft>Warning</Microsoft>
        <MyApp.Something.Tricky>Verbose</MyApp.Something.Tricky>
      </override>
    </minimumLevel>
    <writeTo name="LiterateConsole">
      <name>LiterateConsole</name>
      <args outputTemplate="[{Timestamp:HH:mm:ss} {SourceContext} [{Level}] {Message}{NewLine}{Exception}" />
    </writeTo>
    <writeTo name="File1">
      <name>File</name>
      <args path="%TEMP%\\Logs\\serilog-configuration-sample-1.txt"
            outputTemplate="{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}" />
    </writeTo>
    <writeTo name="File2">
      <name>File</name>
      <args path="%TEMP%\\Logs\\serilog-configuration-sample-2.txt"
            outputTemplate="{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}" />
    </writeTo>
    <enrich name="0">FromLogContext</enrich>
    <enrich name="1">WithMachineName</enrich>
    <enrich name="2">WithThreadId</enrich>
    <properties>
      <Application>Sample</Application>
    </properties>
  </serilog>
</settings>
```

This examples rely on the _Serilog.Sinks.Literate_, _Serilog.Sinks.File_, _Serilog.Enrichers.Environment_ and _Serilog.Enrichers.Thread_ packages also being installed.

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
"WriteTo": ["LiterateConsole", "DiagnosticTrace"]
```

Or alternatively, the long-form (`"Name":` ...) sytax from the first example can be used when arguments need to be supplied.

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

If your application enables the environment variable configuration source (`AddEnvironmentVariables()` via _Microsoft.Extensions.Configuration.EnvironmentVariables_ package) you can add or override Serilog configuration through the environment.

For example, to set the minimum log level using the _Windows_ command prompt:

```
set Serilog:MinimumLevel=Debug
dotnet run
```
