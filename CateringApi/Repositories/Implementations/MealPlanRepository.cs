using CateringApi.Data;
using CateringApi.DTOs.MealPlan;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class MealPlanRepository : IMealPlanRepository
    {
        private readonly DapperContext _context;

        public MealPlanRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveMealPlanAsync(MealPlanSaveDto dto)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@CompanyId", dto.CompanyId);
            param.Add("@MealTypeId", dto.MealTypeId);
            param.Add("@FromDate", dto.FromDate.Date);
            param.Add("@ToDate", dto.ToDate.Date);
            param.Add("@Qty", dto.Qty);
            param.Add("@Remarks", dto.Remarks);
            param.Add("@UserId", dto.UserId);

            await con.ExecuteAsync(
                "dbo.sp_SaveCompanyMealPlan",
                param,
                commandType: CommandType.StoredProcedure);

            return true;
        }

        public async Task<bool> SaveMealPlanOverrideAsync(MealPlanOverrideSaveDto dto)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@CompanyId", dto.CompanyId);
            param.Add("@MealTypeId", dto.MealTypeId);
            param.Add("@FromDate", dto.FromDate.Date);
            param.Add("@ToDate", dto.ToDate.Date);
            param.Add("@Qty", dto.Qty);
            param.Add("@ReasonText", dto.ReasonText);
            param.Add("@UserId", dto.UserId);

            await con.ExecuteAsync(
                "dbo.sp_SaveCompanyMealPlanOverride",
                param,
                commandType: CommandType.StoredProcedure);

            return true;
        }

        public async Task<IEnumerable<DailyMealPlanDto>> GetDailyMealPlanAsync(DateTime planDate, int? companyId, int? mealTypeId)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@PlanDate", planDate.Date);
            param.Add("@CompanyId", companyId);
            param.Add("@MealTypeId", mealTypeId);

            return await con.QueryAsync<DailyMealPlanDto>(
                "dbo.sp_GetDailyMealPlan",
                param,
                commandType: CommandType.StoredProcedure);
        }
    }
}