using Aichmee.Shared;
using Microsoft.Azure.Functions.Worker.Http;

namespace AichmeeLab.Api.Services.AuthenticatorService
{
    public interface IAuthenticatorService
    {

        ServiceResponse<string> AuthorizeUser(HttpRequestData req ,string keyword);

        Task<ServiceResponse<bool>> CheckAuthorization(HttpRequestData req);
    
    
    }
}