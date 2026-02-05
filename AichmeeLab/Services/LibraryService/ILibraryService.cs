using Aichmee.Shared;

namespace AichmeeLab.Services.LibraryService
{
    public interface ILibraryService
    {


        Task<ServiceResponse<string>> GetStatus();
    }
}
