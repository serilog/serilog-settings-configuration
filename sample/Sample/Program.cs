using System;

using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logger.Information("Args: {a}", args);

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
                    new { TenItems = new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" } });

                Console.WriteLine("\nPress \"q\" to quit, or any other key to run again.\n");
            }
            while (!args.Contains("--run-once") && (Console.ReadKey().KeyChar != 'q'));
        }
    }

    // The filter syntax in the sample configuration file is
    // processed by the Serilog.Filters.Expressions package.
    public class CustomFilter : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent)
        {
            return true;
        }
    }
}
