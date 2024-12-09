using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Settings.Configuration.Assemblies;
using Serilog.Settings.Configuration.Tests.Support;
using static Serilog.Settings.Configuration.Tests.Support.ConfigurationReaderTestHelpers;

namespace Serilog.Settings.Configuration.Tests;

public class ConfigurationReaderTests
{
    readonly ConfigurationReader _configurationReader;

    public ConfigurationReaderTests()
    {
        _configurationReader = new ConfigurationReader(
            JsonStringConfigSource.LoadSection("{ \"Serilog\": {  } }", "Serilog"),
            AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies),
            new ConfigurationReaderOptions());
    }

    [Fact]
    public void WriteToSupportSimplifiedSyntax()
    {
        // language=json
        var json = """
        {
            "WriteTo": [ "LiterateConsole", "DiagnosticTrace" ]
        }
        """;

        var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains("LiterateConsole"));
        Assert.True(result.Contains("DiagnosticTrace"));

        Assert.Single(result["LiterateConsole"]);
        Assert.Single(result["DiagnosticTrace"]);
    }

    [Fact]
    public void WriteToSupportExpandedSyntaxWithoutArgs()
    {
        // language=json
        var json = """
        {
            "WriteTo": [ {
                "Name": "LiterateConsole"
            }]
        }
        """;

        var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));
        Assert.Equal(1, result.Count);
        Assert.True(result.Contains("LiterateConsole"));

        Assert.Single(result["LiterateConsole"]);
    }

    [Fact]
    public void WriteToSupportExpandedSyntaxWithArgs()
    {
        // language=json
        var json = """
        {
            "WriteTo": [ {
                "Name": "LiterateConsole",
                "Args": {
                    "outputTemplate": "{Message}"
                }
            }]
        }
        """;

        var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));

        Assert.Single(result);
        Assert.True(result.Contains("LiterateConsole"));

        Assert.Single(result["LiterateConsole"]);

        var args = result["LiterateConsole"].Single().ToArray();

        Assert.Single(args);
        Assert.Equal("outputTemplate", args[0].Key);
        Assert.Equal("{Message}", args[0].Value.ConvertTo(typeof(string), new ResolutionContext()));
    }

    [Fact]
    public void WriteToSupportMultipleSinksOfTheSameKind()
    {
        // language=json
        var json = """
        {
            "WriteTo": [
              {
                "Name": "LiterateConsole",
                "Args": {
                    "outputTemplate": "{Message}"
                  }
              },
              "DiagnosticTrace"
            ],
            "WriteTo:File1": {
                "Name": "File",
                "Args": {
                    "outputTemplate": "{Message}"
                }
            },
            "WriteTo:File2": {
                "Name": "File",
                "Args": {
                    "outputTemplate": "{Message}"
                }
            }
        }
        """;

        var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));

        Assert.Equal(3, result.Count);
        Assert.True(result.Contains("LiterateConsole"));
        Assert.True(result.Contains("DiagnosticTrace"));
        Assert.True(result.Contains("File"));

        Assert.Single(result["LiterateConsole"]);
        Assert.Single(result["DiagnosticTrace"]);
        Assert.Equal(2, result["File"].Count());
    }

    [Fact]
    public void Enrich_SupportSimplifiedSyntax()
    {
        // language=json
        var json = """
        {
            "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
        }
        """;

        var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "Enrich"));
        Assert.Equal(3, result.Count);
        Assert.True(result.Contains("FromLogContext"));
        Assert.True(result.Contains("WithMachineName"));
        Assert.True(result.Contains("WithThreadId"));

        Assert.Single(result["FromLogContext"]);
        Assert.Single(result["WithMachineName"]);
        Assert.Single(result["WithThreadId"]);
    }

    [Fact]
    public void CallableMethodsAreSelected()
    {
        var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
        Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
        var suppliedArgumentNames = new[] { "pathFormat" };

        var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
        Assert.Equal(typeof(string), selected?.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void MethodsAreSelectedBasedOnCountOfMatchedArguments()
    {
        var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
        Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));

        var suppliedArgumentNames = new[] { "pathFormat", "formatter" };

        var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
        Assert.Equal(typeof(ITextFormatter), selected?.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void MethodsAreSelectedBasedOnCountOfMatchedArgumentsAndThenStringType()
    {
        var options = typeof(DummyLoggerConfigurationWithMultipleMethodsExtensions).GetTypeInfo().DeclaredMethods.ToList();
        Assert.Equal(3, options.Count(mi => mi.Name == "DummyRollingFile"));

        var suppliedArgumentNames = new[] { "pathFormat", "formatter" };

        var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
        Assert.Equal(typeof(string), selected?.GetParameters()[2].ParameterType);
    }

    public static IEnumerable<object[]> FlatMinimumLevel =>
    [
        [GetConfigRoot(appsettingsJsonLevel: minimumLevelFlatTemplate.Format(LogEventLevel.Error)), LogEventLevel.Error],
        [GetConfigRoot(appsettingsDevelopmentJsonLevel: minimumLevelFlatTemplate.Format(LogEventLevel.Error)), LogEventLevel.Error],
        [GetConfigRoot(envVariables: new Dictionary<string, string?> {{minimumLevelFlatKey, LogEventLevel.Error.ToString()}}), LogEventLevel.Error],
        [GetConfigRoot(
                appsettingsJsonLevel: minimumLevelFlatTemplate.Format(LogEventLevel.Debug),
                envVariables: new Dictionary<string, string?> {{minimumLevelFlatKey, LogEventLevel.Error.ToString()}}),
            LogEventLevel.Error
        ]
    ];

    [Theory]
    [MemberData(nameof(FlatMinimumLevel))]
    public void FlatMinimumLevelCorrectOneIsEnabledOnLogger(IConfigurationRoot root, LogEventLevel expectedMinimumLevel)
    {
        var reader = new ConfigurationReader(root.GetSection("Serilog"), AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies), new ConfigurationReaderOptions(), root);
        var loggerConfig = new LoggerConfiguration();

        reader.Configure(loggerConfig);

        AssertLogEventLevels(loggerConfig, expectedMinimumLevel);
    }

    public static IEnumerable<object[]> ObjectMinimumLevel =>
    [
        [GetConfigRoot(appsettingsJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Error)), LogEventLevel.Error],
        [GetConfigRoot(appsettingsJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Error.ToString().ToUpper())), LogEventLevel.Error],
        [GetConfigRoot(appsettingsDevelopmentJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Error)), LogEventLevel.Error],
        [GetConfigRoot(envVariables: new Dictionary<string, string?>{{minimumLevelObjectKey, LogEventLevel.Error.ToString() } }), LogEventLevel.Error],
        [GetConfigRoot(
            appsettingsJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Error),
            appsettingsDevelopmentJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Debug)),
            LogEventLevel.Debug ]
    ];

    [Theory]
    [MemberData(nameof(ObjectMinimumLevel))]
    public void ObjectMinimumLevelCorrectOneIsEnabledOnLogger(IConfigurationRoot root, LogEventLevel expectedMinimumLevel)
    {
        var reader = new ConfigurationReader(root.GetSection("Serilog"), AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies), new ConfigurationReaderOptions(), root);
        var loggerConfig = new LoggerConfiguration();

        reader.Configure(loggerConfig);

        AssertLogEventLevels(loggerConfig, expectedMinimumLevel);
    }

    // currently only works in the .NET 4.6.1 and .NET Standard builds of Serilog.Settings.Configuration
    public static IEnumerable<object[]> MixedMinimumLevel =>
    [
        [
            GetConfigRoot(
                appsettingsJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Error),
                appsettingsDevelopmentJsonLevel: minimumLevelFlatTemplate.Format(LogEventLevel.Debug)),
            LogEventLevel.Debug
        ],
        [
            GetConfigRoot(
                appsettingsJsonLevel: minimumLevelFlatTemplate.Format(LogEventLevel.Error),
                appsettingsDevelopmentJsonLevel: minimumLevelObjectTemplate.Format(LogEventLevel.Debug)),
            LogEventLevel.Debug
        ],
        // precedence should be flat > object if from the same source
        [
            GetConfigRoot(
                envVariables: new Dictionary<string, string?>()
                {
                    {minimumLevelObjectKey, LogEventLevel.Error.ToString()},
                    {minimumLevelFlatKey, LogEventLevel.Debug.ToString()}
                }),
            LogEventLevel.Debug
        ]
    ];

    [Theory]
    [MemberData(nameof(MixedMinimumLevel))]
    public void MixedMinimumLevelCorrectOneIsEnabledOnLogger(IConfigurationRoot root, LogEventLevel expectedMinimumLevel)
    {
        var reader = new ConfigurationReader(root.GetSection("Serilog"), AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies), new ConfigurationReaderOptions(), root);
        var loggerConfig = new LoggerConfiguration();

        reader.Configure(loggerConfig);

        AssertLogEventLevels(loggerConfig, expectedMinimumLevel);
    }

    [Fact]
    public void NoConfigurationRootUsedStillValid()
    {
        // language=json
        var json = """
        {
            "Nest": {
                "Serilog": {
                    "MinimumLevel": "Error"
                }
            }
        }
        """;

        var section = JsonStringConfigSource.LoadSection(json, "Nest");
        var reader = new ConfigurationReader(section.GetSection("Serilog"), AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies), new ConfigurationReaderOptions(), section);
        var loggerConfig = new LoggerConfiguration();

        reader.Configure(loggerConfig);

        AssertLogEventLevels(loggerConfig, LogEventLevel.Error);
    }
}
