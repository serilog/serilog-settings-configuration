using System;

using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

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

            do
            {
                logger.ForContext<Program>().Information("Hello, world!");
                logger.ForContext(Constants.SourceContextPropertyName, "Microsoft").Warning("Hello, world!");
                logger.ForContext(Constants.SourceContextPropertyName, "MyApp.Something.Tricky").Verbose("Hello, world!");

                Console.WriteLine();
            }
            while (Console.ReadKey().KeyChar != 'q');
        }
    }

    public class CustomFilter : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent)
        {
            return true;
        }
    }
}
