using CateringApi.Data;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class CuisineRepository : ICuisineRepository
    {
        private readonly DapperContext _context;

        public CuisineRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CuisineDto>> GetAllAsync()
        {
            const string sql = @"
SELECT
    Id,
    CuisineName,
    Description,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate
FROM dbo.CuisineMaster
WHERE IsActive = 1
ORDER BY CuisineName;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<CuisineDto>(sql);
        }

        public async Task<CuisineDto?> GetByIdAsync(int id)
        {
            const string sql = @"
SELECT
    Id,
    CuisineName,
    Description,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate
FROM dbo.CuisineMaster
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<CuisineDto>(sql, new { Id = id });
        }

        public async Task<CuisineDto?> GetByNameAsync(string cuisineName)
        {
            const string sql = @"
SELECT
    Id,
    CuisineName,
    Description,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate
FROM dbo.CuisineMaster
WHERE LTRIM(RTRIM(CuisineName)) = LTRIM(RTRIM(@CuisineName))
  AND IsActive = 1;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<CuisineDto>(sql, new { CuisineName = cuisineName });
        }

        public async Task<bool> NameExistsAsync(string cuisineName, int excludeId)
        {
            const string sql = @"
SELECT 1
FROM dbo.CuisineMaster
WHERE IsActive = 1
  AND Id <> @ExcludeId
  AND UPPER(LTRIM(RTRIM(CuisineName))) = UPPER(LTRIM(RTRIM(@CuisineName)));";

            using var con = _context.CreateConnection();
            var result = await con.QueryFirstOrDefaultAsync<int?>(
                sql,
                new { CuisineName = cuisineName, ExcludeId = excludeId });

            return result.HasValue;
        }

        public async Task<int> SaveAsync(Cuisine dto)
        {
            using var con = _context.CreateConnection();

            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                const string updateSql = @"
UPDATE dbo.CuisineMaster
SET
    CuisineName = @CuisineName,
    Description = @Description,
    IsActive = @IsActive,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;

SELECT @Id;";

                return await con.ExecuteScalarAsync<int>(updateSql, dto);
            }
            else
            {
                const string insertSql = @"
INSERT INTO dbo.CuisineMaster
(
    CuisineName,
    Description,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @CuisineName,
    @Description,
    @IsActive,
    @UserId,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await con.ExecuteScalarAsync<int>(insertSql, dto);
            }
        }

        public async Task<bool> DeleteAsync(int id, int? userId)
        {
            const string sql = @"
UPDATE dbo.CuisineMaster
SET IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }
    }
}