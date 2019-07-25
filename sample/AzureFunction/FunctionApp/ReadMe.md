# Function App - Azure Function Sample
This sample demonstrates using the "serilog-settings-configuration" to load the dependencies for an Azure Function. This will
load the enrichment "FromLogContext" from `appsettings.json` and output the LogContext property "SessionId" in the console. This is
defined in the `appsettings.json` in the `WriteTo` property for the "Console" as `[{SessionId}]`:
```json
{
    "Args": { "outputTemplate": "{Timestamp} [{Level:u3}] [{SessionId}] {Message}{NewLine}{Exception}" }
}
```

## Setup
This function is also using dependency injection to create the Serilog.ILogger object. The creation of the Logger is in the `Startup.cs` file.

## Dependencies
```xml
    <PackageReference Include="Microsoft.Azure.Functions.Extensions" Version="1.0.0" />
    <PackageReference Include="Microsoft.NET.Sdk.Functions" Version="1.0.29" />
    <PackageReference Include="Serilog.Enrichers.Environment" Version="2.1.3" />
    <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="3.0.3" />
    <PackageReference Include="Serilog.Sinks.Console" Version="3.1.1" />
```

## Running the Application 
1. Clone Repository
1. Create a `local.settings.json` file in the root of the directory with the following configuration.
    ```json
    {
        "IsEncrypted": false,
        "Values": {
            "AzureWebJobsStorage": "UseDevelopmentStorage=true",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet"
        }
    }
    ```

1. *Optional* - Application Insights. To see the results published to your Application Insights container: Update the `appsettings.json` setting the values for ApplicationInsights.

    ### Azure Function Settings
    ```json
    {
    "ApplicationInsights": {
        "InstrumentationKey": "[ENTER VALUE]"
    }
    }
    ```

    ### Serilog Configuration
    ```json

    {
        "Name": "ApplicationInsights",
        "Args": {
            "restrictedToMinimumLevel": "Verbose",
            "instrumentationKey": "[ENTER VALUE]"
        }
    }
    ```
1. Build and Run
1. Make a `GET` request to: [http://localhost:7071/api/MyFunction?name=Sheep)
