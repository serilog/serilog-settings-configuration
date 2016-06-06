using System;

using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

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
                logger.Information("Hello, world!");
            }
            while (Console.ReadKey().KeyChar != 'q');
        }
    }
}
