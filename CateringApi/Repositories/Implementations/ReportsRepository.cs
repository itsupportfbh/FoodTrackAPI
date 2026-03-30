using CateringApi.Data;
using CateringApi.DTOs.Reports;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly DapperContext _context;

        public ReportsRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderedVsScannedDto>> GetOrderedVsScannedAsync(DateTime fromDate, DateTime toDate, int? companyId, int? mealTypeId)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@FromDate", fromDate.Date);
            param.Add("@ToDate", toDate.Date);
            param.Add("@CompanyId", companyId);
            param.Add("@MealTypeId", mealTypeId);

            return await con.QueryAsync<OrderedVsScannedDto>(
                "dbo.sp_Report_OrderedVsScanned",
                param,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<InvalidScanDto>> GetInvalidScansAsync(DateTime fromDate, DateTime toDate)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@FromDate", fromDate.Date);
            param.Add("@ToDate", toDate.Date);

            return await con.QueryAsync<InvalidScanDto>(
                "dbo.sp_Report_InvalidOrDuplicateScans",
                param,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<MissedMealDto>> GetMissedMealsAsync(DateTime fromDate, DateTime toDate, int? companyId)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@FromDate", fromDate.Date);
            param.Add("@ToDate", toDate.Date);
            param.Add("@CompanyId", companyId);

            return await con.QueryAsync<MissedMealDto>(
                "dbo.sp_Report_MissedMeals",
                param,
                commandType: CommandType.StoredProcedure);
        }
    }
}