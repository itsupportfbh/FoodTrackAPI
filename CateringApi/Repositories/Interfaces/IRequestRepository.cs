using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IRequestRepository
    {
        Task<RequestPageMasterDto> GetPageMastersAsync(int userId);
        Task<IEnumerable<RequestDto>> GetAllRequestsAsync(int userId);
        Task<RequestDto?> GetRequestByIdAsync(int id);
        Task<int> SaveRequestAsync(RequestHeaderDto model);
        Task<bool> DeleteRequestAsync(int id, int? userId);
        Task<int> GetOrderDays();
        Task<bool> CheckOverlapAsync(int companyId, DateTime fromDate, DateTime toDate, int id = 0);
        Task<IEnumerable<PlanUserCountDto>> GetPlanUserCountsAsync(int companyId);
    }
}