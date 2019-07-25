using System.Reflection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

[assembly: WebJobsStartup(typeof(FunctionApp.Startup))]

namespace FunctionApp {
    sealed class Startup : FunctionsStartup {

        /// <summary>
        /// Configures the specified builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public override void Configure(IFunctionsHostBuilder builder) {

            // Add dependency injection for the ILogger.
            builder.Services.AddSingleton<ILogger>(logger => GetSeriLogger());
        }

        /// <summary>
        /// Gets the Serilog Logger.
        /// </summary>
        /// <returns><see cref="Serilog.ILogger"/>.</returns>
        ILogger GetSeriLogger() {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(GetConfiguration(), Assembly.GetAssembly(typeof(Startup)))
                .CreateLogger();

            // At this point, you should see logger._enricher contain 4 values in it's collection.
            return logger;
        }

        /// <summary>
        /// Gets the constructed <see cref="IConfigurationRoot"/> instance.
        /// </summary>
        /// <returns>IConfigurationRoot.</returns>
        IConfigurationRoot GetConfiguration() {

            var environmentSettings = $"appsettings.{AzureVariables.AzureFunctionsEnvironment}.json";

            return new ConfigurationBuilder()
                .SetBasePath(FunctionRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(environmentSettings, optional: true)
                .AddEnvironmentVariables()
                .Build();
        }


        /// <summary>
        /// Gets the function root directory path.
        /// </summary>
        /// <value>The function root directory path.</value>
        string FunctionRootPath =>
            AzureVariables.AzureWebJobsScriptRoot ?? $"{AzureVariables.HomeDirectory}/site/wwwroot";
    }
}
