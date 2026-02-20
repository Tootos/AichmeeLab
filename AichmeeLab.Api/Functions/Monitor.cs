using Aichmee.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Extensions.Logging;
using System.Net;
using Newtonsoft.Json;
using MongoDB.Bson.IO;


namespace AichmeeLab.Api.Functions
{
    public class Monitor
    {
        private readonly ILogger<Monitor> _logger;

        public Monitor(ILogger<Monitor> logger)
        {
            _logger = logger;
        }

        

    }
}
