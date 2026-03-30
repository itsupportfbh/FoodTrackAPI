using CateringApi.DTOs.Common;
using CateringApi.DTOs.MealPlan;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealPlanController : ControllerBase
    {
        private readonly IMealPlanRepository _repository;

        public MealPlanController(IMealPlanRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("save-base-plan")]
        public async Task<IActionResult> SaveBasePlan([FromBody] MealPlanSaveDto dto)
        {
            await _repository.SaveMealPlanAsync(dto);
            return Ok(ApiResponse<string>.Ok(null, "Base meal plan saved successfully"));
        }

        [HttpPost("save-override")]
        public async Task<IActionResult> SaveOverride([FromBody] MealPlanOverrideSaveDto dto)
        {
            await _repository.SaveMealPlanOverrideAsync(dto);
            return Ok(ApiResponse<string>.Ok(null, "Meal plan override saved successfully"));
        }

        [HttpGet("daily-plan")]
        public async Task<IActionResult> GetDailyPlan(
            [FromQuery] DateTime planDate,
            [FromQuery] int? companyId,
            [FromQuery] int? mealTypeId)
        {
            var data = await _repository.GetDailyMealPlanAsync(planDate, companyId, mealTypeId);
            return Ok(ApiResponse<IEnumerable<DailyMealPlanDto>>.Ok(data));
        }
    }
}