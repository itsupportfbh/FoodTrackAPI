using CateringApi.DTOs.RequestOverride;

namespace CateringApi.Repositories.Interfaces
{
    public interface IRequestOverrideRepository
    {
        Task<RequestOverrideScreenDto?> GetScreenDataAsync(int requestHeaderId, DateTime fromDate, DateTime toDate);
        Task<int> SaveAsync(SaveRequestOverrideDto dto);
        Task<IEnumerable<dynamic>> GetOverrideListAsync(int requestHeaderId);
        Task DeleteAsync(int id, int updatedBy);
    }
}
