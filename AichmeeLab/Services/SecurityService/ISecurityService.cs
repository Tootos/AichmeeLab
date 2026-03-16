using Aichmee.Shared;

namespace AichmeeLab.Services.SecurityService
{
    
public interface ISecurityService
    {

        Task<ServiceResponse<bool>> GetAdminAccessAsync(string keyword);


        Task<bool> IsAuthorizedAsync();
    }
}