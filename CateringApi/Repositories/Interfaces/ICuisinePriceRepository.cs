using CateringApi.DTOs;

namespace CateringApi.Repositories.Interfaces
{
    public interface ICuisinePriceRepository
    {
        Task<IEnumerable<CuisineRateViewModel>> GetAllCuisinesWithRatesAsync(int companyId, int sessionId);
        Task<IEnumerable<CuisineRateViewModel>> GetCuisineRatesByCompanySessionAsync(int companyId, int sessionId);
        Task<bool> SaveBulkCuisinePricesAsync(BulkCuisinePriceSaveRequest request);
        Task<IEnumerable<CuisinePriceHistoryDto>> GetCuisinePriceHistoryAsync(int companyId, int sessionId, int cuisineId);

        Task<decimal> GetApplicableCuisineRateAsync(int companyId, int sessionId, int cuisineId, DateTime orderDate);
    }
}
