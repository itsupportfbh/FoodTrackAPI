using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IRequestRepository
    {
        Task<RequestPageMasterDto> GetPageMastersAsync(int userId);
        Task<IEnumerable<RequestDto>> GetAllRequestsAsync(int userId);
        Task<RequestDto?> GetRequestByIdAsync(int requestId);
        Task<int> SaveRequestAsync(Request model);
        Task<bool> DeleteRequestAsync(int requestId, int? userId);
        Task<bool> ExistsDuplicateAsync(Request model);
    }
}