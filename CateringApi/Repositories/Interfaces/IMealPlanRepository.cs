using CateringApi.DTOs.MealPlan;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMealPlanRepository
    {
        Task<bool> SaveMealPlanAsync(MealPlanSaveDto dto);
        Task<bool> SaveMealPlanOverrideAsync(MealPlanOverrideSaveDto dto);
        Task<IEnumerable<DailyMealPlanDto>> GetDailyMealPlanAsync(DateTime planDate, int? companyId, int? mealTypeId);
    }
}