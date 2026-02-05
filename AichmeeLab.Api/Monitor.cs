using Aichmee.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Extensions.Logging;
using System.Net;


namespace AichmeeLab.Api
{
    public class Monitor
    {
        private readonly ILogger<Monitor> _logger;

        public Monitor(ILogger<Monitor> logger)
        {
            _logger = logger;
        }

        [Function("GetMessage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetMessage")] HttpRequest req)
        {
            _logger.LogInformation("Getting a status message");

            var response = new ServiceResponse<string>
            {
                Data = "Hello world"
            };

            return new  OkObjectResult(response);
        }
    }
}
