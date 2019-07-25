using System;

namespace FunctionApp {
    /// <summary>
    ///  Helper class for working with Azure Environment Variables.
    ///  <see href="https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings"/>
    /// </summary>
    public static class AzureVariables {

        /// <summary>
        /// The azure web jobs script root variable
        /// </summary>
        public static string AzureWebJobsScriptRootVariable = "AzureWebJobsScriptRoot";

        /// <summary>
        /// The home directory variable
        /// </summary>
        public static string HomeDirectoryVariable = "HOME";

        /// <summary>
        /// The azure functions environment variable
        /// </summary>
        public static string AzureFunctionsEnvironmentVariable = "AZURE_FUNCTIONS_ENVIRONMENT";

        /// <summary>
        /// Gets the get azure web jobs script root.
        /// </summary>
        /// <value>The get azure web jobs script root.</value>
        public static string AzureWebJobsScriptRoot => GetValue(AzureWebJobsScriptRootVariable);

        /// <summary>
        /// Gets the get home directory.
        /// </summary>
        /// <value>The get home directory.</value>
        public static string HomeDirectory => GetValue(HomeDirectoryVariable);

        /// <summary>
        /// Gets the get azure functions environment.
        /// </summary>
        /// <value>The get azure functions environment.</value>
        public static string AzureFunctionsEnvironment => GetValue(AzureFunctionsEnvironmentVariable);

        /// <summary>
        /// Gets the value from the environment variables.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private static string GetValue(string key) {
            return Environment.GetEnvironmentVariable(key);
        }
    }
}
