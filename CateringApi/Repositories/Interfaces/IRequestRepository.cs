using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IRequestRepository
    {
        Task<RequestPageMasterDto> GetPageMastersAsync(int userId);
        Task<IEnumerable<RequestDto>> GetAllRequestsAsync(int userId);
        Task<RequestDto?> GetRequestByIdAsync(int id);
        Task<int> SaveRequestAsync(RequestHeader model);
        Task<bool> DeleteRequestAsync(int id, int? userId);
    }
}