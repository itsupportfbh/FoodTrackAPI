using CateringApi.Data;
using CateringApi.DTOs.MealType;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class MealTypeRepository : IMealTypeRepository
    {
        private readonly DapperContext _context;

        public MealTypeRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MealTypeDto>> GetAllAsync()
        {
            const string sql = @"
SELECT
    Id, MealTypeCode, MealTypeName, StartTime, EndTime, IsActive
FROM dbo.MealTypeMaster
WHERE IsActive = 1
ORDER BY Id;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<MealTypeDto>(sql);
        }
    }
}