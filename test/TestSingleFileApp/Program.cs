using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;

try
{
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
