﻿using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Settings.Configuration;

if (args.Length == 1 && args[0] == "is-single-file")
{
    if (typeof(Program).Assembly.GetManifestResourceNames().Any(e => e.StartsWith("costura.")))
    {
        Console.WriteLine(true);
        return 0;
    }
    // IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app
#pragma warning disable IL3000
    Console.WriteLine(string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location));
#pragma warning restore
    return 0;
}

SelfLog.Enable(Console.Error);

Thread.CurrentThread.Name = "Main thread";
const string outputTemplate = "({ThreadName}) [{Level}] {Message}{NewLine}";

var configurationValues = new Dictionary<string, string?>();
var minimumLevelOnly = args.Contains("--minimum-level-only");
if (minimumLevelOnly)
{
    configurationValues["Serilog:MinimumLevel"] = "Verbose";
}
else
{
    configurationValues["Serilog:Enrich:0"] = "WithThreadName";
    configurationValues["Serilog:WriteTo:0:Name"] = "Console";
    configurationValues["Serilog:WriteTo:0:Args:outputTemplate"] = outputTemplate;
}

if (args.Contains("--using-thread")) configurationValues["Serilog:Using:Thread"] = "Serilog.Enrichers.Thread";
if (args.Contains("--using-console")) configurationValues["Serilog:Using:Console"] = "Serilog.Sinks.Console";

var assemblies = new List<Assembly>();
if (args.Contains("--assembly-thread")) assemblies.Add(typeof(ThreadLoggerConfigurationExtensions).Assembly);
if (args.Contains("--assembly-console")) assemblies.Add(typeof(ConsoleLoggerConfigurationExtensions).Assembly);

try
{
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurationValues).Build();
    var options = assemblies.Count > 0 ? new ConfigurationReaderOptions(assemblies.ToArray()) : null;
    var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration, options);
    if (minimumLevelOnly)
    {
        loggerConfiguration
            .Enrich.WithThreadName()
            .WriteTo.Console(outputTemplate: outputTemplate);
    }
    var logger = loggerConfiguration.CreateLogger();
    logger.Information("Expected success");
    return 0;
}
catch (InvalidOperationException exception) when (exception.Message.StartsWith("No Serilog:Using configuration section is defined and no Serilog assemblies were found."))
{
    Console.WriteLine("Expected exception");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
