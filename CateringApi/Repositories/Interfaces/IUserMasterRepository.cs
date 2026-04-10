using CateringApi.DTOs;
using CateringApi.DTOs.User;

namespace CateringApi.Repositories.Interfaces
{
    public interface IUserMasterRepository
    {
        Task<IEnumerable<UserMasterDTO>> GetAllAsync(long currentUserId, int currentRoleId, int currentCompanyId);
        Task<UserMaster> GetByIdAsync(long id);
        Task<int> CreateAsync(CreateUserMasterDto userMaster);
        Task UpdateAsync(UserMaster userMaster);
        Task DeleteAsync(int id, int updatedBy);
    }
}