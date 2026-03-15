using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;

namespace AichmeeLab.Api.Middleware
{
     public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context
                               , FunctionExecutionDelegate next)
        {
            // TODO: Add authentication logic here if needed
            var requestData = await context.GetHttpRequestDataAsync();

            if (requestData != null)
            {
                var path = requestData.Url.PathAndQuery;

                // Safeguard: Only intercept Dashboard routes
                if (path.Contains("/api/dashboard/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsAuthorized(requestData))
                    {
                        var response = requestData.CreateResponse(HttpStatusCode.Unauthorized);
                        await response.WriteAsJsonAsync(new { Success = false, Message = "Access Denied" });
                        
                        // Sets the result so the Function doesn't execute
                        context.GetInvocationResult().Value = response;
                        return; 
                    }
                }
            }

            await next(context);
        }

        private bool IsAuthorized(HttpRequestData req)
        {
            if (req.Headers.TryGetValues("Cookie", out var cookies))
            {
                var cookieString = string.Join("; ", cookies);
                // Look for your specific authorized token
                return cookieString.Contains("AdminSession=Authorized");
            }
            return false;
        }
    }
}