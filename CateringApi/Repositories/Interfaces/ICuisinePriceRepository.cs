using CateringApi.DTOs;

namespace CateringApi.Repositories.Interfaces
{
    public interface ICuisinePriceRepository
    {
        Task<IEnumerable<CuisineRateViewModel>> GetAllCuisinesWithRatesAsync(int companyId, int sessionId);
        Task<SessionRateViewDto?> GetSessionRateAsync(int companyId, int sessionId);
        Task<bool> SaveSessionRateAsync(SessionRateSaveRequest request);
        Task<IEnumerable<CuisinePriceHistoryDto>> GetCuisinePriceHistoryAsync(int companyId, int sessionId, int cuisineId);

        Task<decimal> GetApplicableCuisineRateAsync(int companyId, int sessionId, int cuisineId, DateTime orderDate);
        Task<List<PriceListDto>> GetPriceList();
    }
}
