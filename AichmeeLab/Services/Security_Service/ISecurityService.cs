using Aichmee.Shared;

namespace AichmeeLab.Services.SecurityService
{
    
public interface ISecurityService
    {

        Task<ServiceResponse<string>> GetAdminAccessAsync(string keyword);


        Task<bool> IsAuthorizedAsync();
    }
}