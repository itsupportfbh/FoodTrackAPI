using CateringApi.DTOs.Reports;

namespace CateringApi.Repositories.Interfaces
{
    public interface IReportsRepository
    {
        Task<IEnumerable<OrderedVsScannedDto>> GetOrderedVsScannedAsync(DateTime fromDate, DateTime toDate, int? companyId, int? mealTypeId);
        Task<IEnumerable<InvalidScanDto>> GetInvalidScansAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<MissedMealDto>> GetMissedMealsAsync(DateTime fromDate, DateTime toDate, int? companyId);
    }
}