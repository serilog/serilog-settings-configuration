using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;

namespace FunctionApp {
    public class MyFunction {

        readonly ILogger _logger;

        public MyFunction(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName("MyFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ExecutionContext executionContext) {

            using (LogContext.PushProperty("SessionId", executionContext.InvocationId)) {
                _logger.Information("C# HTTP trigger function processed a request.");

                string name = req.Query["name"];

                _logger.Debug("Resolved Name to {@name}", name);

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;

                return name != null
                    ? (ActionResult) new OkObjectResult($"Hello, {name}")
                    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
        }
    }
}
