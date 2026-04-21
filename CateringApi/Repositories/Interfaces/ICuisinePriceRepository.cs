using CateringApi.DTOs;
using CateringApi.Repositories.Implementations;
using static CateringApi.Repositories.Implementations.CuisinePriceRepository;

namespace CateringApi.Repositories.Interfaces
{
    public interface ICuisinePriceRepository
    {
        Task<IEnumerable<SessionDropdownDto>> GetAllSessionsAsync();
        Task<bool> SaveDefaultPlanRatesBulkAsync(DefaultPlanBulkSaveRequest request);
        Task<List<CompanyPlanRateViewDto>> GetDefaultPlanRatesAsync();
        Task<List<PriceListDto>> GetPriceList();
        Task<IEnumerable<CuisinePriceHistoryDto>> GetDefaultPriceHistoryAsync(int sessionId, string planType);
    }
}
