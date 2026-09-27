using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using AichmeeLab.Api.LocalModels;
using Aichmee.Shared;

namespace AichmeeLab.Api.Utilities
{
    public static class MyMethods
    {
        public static async Task<HttpResponseData> ReturnResponseAsync<T>(
                            HttpRequestData req,
                            ServiceResponse<T> serviceResult)
        {
            if (serviceResult.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(serviceResult);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(serviceResult);
            return badRequest;
        }

        public static async Task<HttpResponseData> FindResponseAsync<T>(
                                    HttpRequestData req,
                                    ServiceResponse<T> serviceResult)
        {
            if (serviceResult.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(serviceResult);
                return response;
            }
            Console.WriteLine("MyMethod says it failed");
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(serviceResult);
            return notFoundResponse;
        }

    }


}