
using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
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
        readonly string _keyword;
        readonly bool _isSecure;
        readonly bool _isLocal;

        public AuthenticatorFunctions(ILogger<AuthenticatorFunctions> logger, IConfiguration config)
        {

            _logger = logger;
            _keyword = config["AuthorizationKeyword"] ?? "default_key";


            _isSecure = config.GetValue<bool>("UseSecure", true);
            _isLocal = config.GetValue<bool>("IsLocal",false);


        }

        [Function("AdminAuth")]
        public async Task<HttpResponseData> Run([
             HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="auth/unlock/{keyword}")]
             HttpRequestData req, string keyword)
        {

            // 1. Grab the IP (Isolated headers are slightly different)
            //This only works when the app is hosted in Azure
            string clientIp = string.Empty;
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
            {
                clientIp = forwardedIps.FirstOrDefault() ?? string.Empty;
            }
            
            //For local development
            if (_isLocal)
            {
             
                clientIp = "127.0.0.1";
            }

            // 2. Initial Guard Clause
            if (string.IsNullOrEmpty(clientIp))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await errorResponse.WriteAsJsonAsync(new ServiceResponse<bool> { Success = false, Data = false, Message = "No IP found in the Request" });
                return errorResponse;
            }



            // 3. Check Keyword
            if (string.IsNullOrEmpty(keyword) || keyword != _keyword)
            {
                // TODO: Update attempts in your Security DB
                var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorized.WriteAsJsonAsync(new ServiceResponse<bool> { Success = false, Data = false, Message = "Incorrect Key" });
                return unauthorized;
            }

            // 4. All checks passed! Create the Success Response
            var response = req.CreateResponse(HttpStatusCode.OK);

            // 5. We create a special cookie object and add it to the response collection
            var adminCookie = new HttpCookie("AdminSession", "Authorized")
            {
                Path = "/",
                HttpOnly = true,
                Secure = _isSecure,
                SameSite = SameSite.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            };

            response.Cookies.Append(adminCookie);

            // 6. Return the JSON
            await response.WriteAsJsonAsync(new ServiceResponse<bool>
            {
                Data = true,
                Success = true
            });

            return response;
        }



        [Function("CheckAuth")]
        public async Task<HttpResponseData> Check([
            HttpTrigger(AuthorizationLevel.Anonymous, "get",Route = "auth/check/" )]
        HttpRequestData req)
        {
            var cookieHeader = req.Headers.TryGetValues("Cookie", out var cookies)
                ? string.Join("; ", cookies)
                : string.Empty;

            if (cookieHeader.Contains("AdminSession=Authorized"))
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ServiceResponse<bool>
                {
                    Data = true,
                    Success = true
                });
                return response;
            }

            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new ServiceResponse<bool>
            {
                Data = false,
                Success = false,
                Message = "Unauthorized Access"
            });
            return unauthorizedResponse;

        }


    }
}