using Microsoft.Extensions.Configuration;
using Sample;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;

// ReSharper disable UnusedType.Global

SelfLog.Enable(Console.Error);

Thread.CurrentThread.Name = "Main thread";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

logger.Information("Args: {Args}", args);

do
{
    logger.ForContext<Program>().Information("Hello, world!");
    logger.ForContext<Program>().Error("Hello, world!");
    logger.ForContext(Constants.SourceContextPropertyName, "Microsoft").Warning("Hello, world!");
    logger.ForContext(Constants.SourceContextPropertyName, "Microsoft").Error("Hello, world!");
    logger.ForContext(Constants.SourceContextPropertyName, "MyApp.Something.Tricky").Verbose("Hello, world!");

    logger.Information("Destructure with max object nesting depth:\n{@NestedObject}",
        new { FiveDeep = new { Two = new { Three = new { Four = new { Five = "the end" } } } } });

    logger.Information("Destructure with max string length:\n{@LongString}",
        new { TwentyChars = "0123456789abcdefghij" });

    logger.Information("Destructure with max collection count:\n{@BigData}",
        new { TenItems = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" } });

    logger.Information("Destructure with policy to strip password:\n{@LoginData}",
        new LoginData { Username = "BGates", Password = "isityearoflinuxyet" });

    Console.WriteLine("\nPress \"q\" to quit, or any other key to run again.\n");
}
while (!args.Contains("--run-once") && (Console.ReadKey().KeyChar != 'q'));
