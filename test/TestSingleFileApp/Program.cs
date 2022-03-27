using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;

try
{
    // Force loading of assemblies could become unnecessary if the [DependencyContextLoader][1]
    // starts supporting applications published as single-file in the future.
    // Unfortunately, as of .NET 6, loading the DependencyContext from a single-file application is not supported.
    // [1]: https://github.com/dotnet/runtime/blob/v6.0.3/src/libraries/Microsoft.Extensions.DependencyModel/src/DependencyContextLoader.cs#L54-L55
    _ = typeof(Serilog.Sinks.InMemory.InMemorySinkExtensions).Assembly;
    _ = typeof(ConsoleLoggerConfigurationExtensions).Assembly;

    SelfLog.Enable(text => Console.Error.WriteLine(text));

    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
    {
        ["Serilog:WriteTo:0:Name"] = "InMemory",
        ["Serilog:WriteTo:1:Name"] = "Console",
        ["Serilog:WriteTo:1:Args:outputTemplate"] = "{Message:l}",
    }).Build();

    using var logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
    logger.Information("Everything is working as expected");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"An unexpected exception occurred: {exception}");
}
