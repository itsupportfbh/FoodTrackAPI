using CateringApi.DTOs;
using CateringApi.DTOs.User;
using CateringApi.Models;
using Microsoft.AspNetCore.Http;

namespace CateringApi.Repositories.Interfaces
{
    public interface IUserMasterRepository
    {
        Task<IEnumerable<UserMasterDTO>> GetAllAsync(long currentUserId, int currentRoleId, int currentCompanyId);
        Task<UserMaster> GetByIdAsync(long id);
        Task<int> CreateAsync(CreateUserMasterDto userMaster);
        Task UpdateAsync(UserMaster1 userMaster);
        Task DeleteAsync(int id, int updatedBy);
        Task<IEnumerable<RolesDTO>> GetRoles();

        Task<byte[]> DownloadUserTemplateAsync(int companyId);
        Task<string> BulkUploadUsersAsync(IFormFile file, int updatedBy, int companyId);

        Task<IEnumerable<CuisineDto>> GetAllCuisine(int companyId);
    }
}