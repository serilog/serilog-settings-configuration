using System;

using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

using Serilog.Core;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
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
}
