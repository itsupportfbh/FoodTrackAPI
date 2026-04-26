using CateringApi.DTOs.MealPlan;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMealRequestRepository
    {
        Task<IEnumerable<MealRequestListDto>> GetAllMealRequests(int companyId, int userId);
        Task<MealRequestListDto?> GetMealRequestById(int id);
        Task<object> SaveMealRequest(SaveMealRequestDto dto);
        Task<object> DeleteMealRequest(int id);

        Task<IEnumerable<ShowQrDTO>> ShowQr(int companyId, int userId);
    }
}
