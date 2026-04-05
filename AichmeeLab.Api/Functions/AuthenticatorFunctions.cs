
using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Services.AuthenticatorService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AichmeeLab.Api
{

    class AuthenticatorFunctions
    {

        readonly ILogger<AuthenticatorFunctions> _logger;
        readonly IAuthenticatorService _authenticatorService;

        public AuthenticatorFunctions(ILogger<AuthenticatorFunctions> logger, IAuthenticatorService authenticatorService)
        {

            _logger = logger;

            _authenticatorService = authenticatorService;

        }

        [Function("AdminAuth")]
        public async Task<HttpResponseData> Run([
             HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="auth/unlock/{keyword}")]
             HttpRequestData req, string keyword)
        {

            var result = await _authenticatorService.AuthorizeUser(req, keyword);
            if (result.Success && result.Data !=null)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                // Only add the header if the Data actually contains the cookie formatting
                if (result.Data.StartsWith("AdminSession="))
                {
                    successResponse.Headers.Add("Set-Cookie", result.Data);
                }

                await successResponse.WriteAsJsonAsync(result);
                return successResponse;
            }

            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(result);
            return unauthorizedResponse;
        }



        [Function("CheckAuth")]
        public async Task<HttpResponseData> Check([
            HttpTrigger(AuthorizationLevel.Anonymous, "get",Route = "auth/check/" )]
        HttpRequestData req)
        {
            var result = await _authenticatorService.CheckAuthorization(req);
            if (result.Success)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(result);
                return successResponse;
            }

            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(result);
            return unauthorizedResponse;


        }
    }
}