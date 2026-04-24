using CateringApi.DTOs.Common;
using CateringApi.DTOs.RequestOverride;

namespace CateringApi.Repositories.Interfaces
{
    public interface IRequestOverrideRepository
    {
        Task<RequestOverrideScreenDto?> GetScreenDataAsync(int requestHeaderId, DateTime fromDate, DateTime toDate);
        Task<SaveRequestOverrideResultDto> SaveAsync(SaveRequestOverrideDto dto);

        Task<List<RequestOverrideListDto>> GetOverrideList(int companyId);
        Task<List<RequestOverrideLineDto>> GetOverrideLines(int requestOverrideId);

        Task DeleteAsync(int id, int updatedBy);
    }
}
