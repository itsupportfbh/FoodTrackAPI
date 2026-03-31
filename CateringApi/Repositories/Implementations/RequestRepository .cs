using CateringApi.Data;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class RequestRepository : IRequestRepository
    {
        private readonly DapperContext _context;

        public RequestRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<RequestPageMasterDto> GetPageMastersAsync(int userId)
        {
            using var con = _context.CreateConnection();

            const string userCompanySql = @"
SELECT CompanyId
FROM dbo.UserMaster
WHERE Id = @UserId
  AND IsActive = 1;";

            var companyId = await con.QueryFirstOrDefaultAsync<int?>(userCompanySql, new { UserId = userId });

            IEnumerable<DropdownDto> companies;
            IEnumerable<DropdownDto> sessions;
            IEnumerable<DropdownDto> cuisines;
            IEnumerable<DropdownDto> locations;

     
                const string companySql = @"
SELECT
    c.Id,
    NULL AS CompanyId,
    c.CompanyName AS Name
FROM dbo.CompanyMaster c
WHERE c.Id = @CompanyId
  AND c.IsActive = 1;";

                const string sessionSql = @"
SELECT DISTINCT
    s.Id,
    csm.CompanyId,
    s.SessionName AS Name
FROM dbo.Session s
INNER JOIN dbo.CompanySessionMap csm ON csm.SessionId = s.Id
WHERE csm.CompanyId = @CompanyId
ORDER BY s.SessionName;";

                const string cuisineSql = @"
SELECT DISTINCT
    c.Id,
    ccm.CompanyId,
    c.CuisineName AS Name
FROM dbo.CuisineMaster c
INNER JOIN dbo.CompanyCuisineMap ccm ON ccm.CuisineId = c.Id
WHERE ccm.CompanyId = @CompanyId
  AND c.IsActive = 1
ORDER BY c.CuisineName;";

                const string locationSql = @"
SELECT DISTINCT
    l.Id,
    clm.CompanyId,
    l.LocationName AS Name
FROM dbo.Location l
INNER JOIN dbo.CompanyLocationMap clm ON clm.LocationId = l.Id
WHERE clm.CompanyId = @CompanyId
  AND l.IsActive = 1
ORDER BY l.LocationName;";

                companies = await con.QueryAsync<DropdownDto>(companySql, new { CompanyId = companyId ?? 0 });
                sessions = await con.QueryAsync<DropdownDto>(sessionSql, new { CompanyId = companyId ?? 0 });
                cuisines = await con.QueryAsync<DropdownDto>(cuisineSql, new { CompanyId = companyId ?? 0 });
                locations = await con.QueryAsync<DropdownDto>(locationSql, new { CompanyId = companyId ?? 0 });
           

            return new RequestPageMasterDto
            {
                Companies = companies,
                Sessions = sessions,
                Cuisines = cuisines,
                Locations = locations
            };
        }

        public async Task<IEnumerable<RequestDto>> GetAllRequestsAsync(int userId)
        {
            using var con = _context.CreateConnection();

            const string userCompanySql = @"
SELECT CompanyId
FROM dbo.UserMaster
WHERE Id = @UserId
  AND IsActive = 1;";

            var companyId = await con.QueryFirstOrDefaultAsync<int?>(userCompanySql, new { UserId = userId });

            string sql;

                sql = @"
SELECT
    r.RequestId,
    r.CompanyId,
    c.CompanyName,
    r.SessionId,
    s.SessionName,
    r.CuisineId,
    cu.CuisineName,
    r.LocationId,
    l.LocationName,
    r.FromDate,
    r.ToDate,
    r.Qty,
    r.IsActive,
    r.CreatedBy,
    r.CreatedDate,
    r.UpdatedBy,
    r.UpdatedDate
FROM dbo.RequestMaster r
INNER JOIN dbo.CompanyMaster c ON c.Id = r.CompanyId
INNER JOIN dbo.Session s ON s.Id = r.SessionId
INNER JOIN dbo.CuisineMaster cu ON cu.Id = r.CuisineId
INNER JOIN dbo.Location l ON l.Id = r.LocationId
WHERE r.IsActive = 1
  AND r.CompanyId = @CompanyId
ORDER BY r.RequestId DESC;";
            

            return await con.QueryAsync<RequestDto>(sql, new { CompanyId = companyId ?? 0 });
        }

        public async Task<RequestDto?> GetRequestByIdAsync(int requestId)
        {
            const string sql = @"
SELECT
    r.RequestId,
    r.CompanyId,
    c.CompanyName,
    r.SessionId,
    s.SessionName,
    r.CuisineId,
    cu.CuisineName,
    r.LocationId,
    l.LocationName,
    r.FromDate,
    r.ToDate,
    r.Qty,
    r.IsActive,
    r.CreatedBy,
    r.CreatedDate,
    r.UpdatedBy,
    r.UpdatedDate
FROM dbo.RequestMaster r
INNER JOIN dbo.CompanyMaster c ON c.Id = r.CompanyId
INNER JOIN dbo.Session s ON s.Id = r.SessionId
INNER JOIN dbo.CuisineMaster cu ON cu.Id = r.CuisineId
INNER JOIN dbo.Location l ON l.Id = r.LocationId
WHERE r.RequestId = @RequestId;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<RequestDto>(sql, new { RequestId = requestId });
        }

        public async Task<bool> ExistsDuplicateAsync(Request model)
        {
            const string sql = @"
SELECT 1
FROM dbo.RequestMaster
WHERE IsActive = 1
  AND CompanyId = @CompanyId
  AND SessionId = @SessionId
  AND CuisineId = @CuisineId
  AND LocationId = @LocationId
  AND FromDate = @FromDate
  AND ToDate = @ToDate
  AND (@RequestId IS NULL OR RequestId <> @RequestId);";

            using var con = _context.CreateConnection();

            var result = await con.QueryFirstOrDefaultAsync<int?>(
                sql,
                new
                {
                    model.RequestId,
                    model.CompanyId,
                    model.SessionId,
                    model.CuisineId,
                    model.LocationId,
                    model.FromDate,
                    model.ToDate
                });

            return result.HasValue;
        }

        public async Task<int> SaveRequestAsync(Request model)
        {
            using var con = _context.CreateConnection();

            if (model.RequestId.HasValue && model.RequestId.Value > 0)
            {
                const string updateSql = @"
UPDATE dbo.RequestMaster
SET
    CompanyId = @CompanyId,
    SessionId = @SessionId,
    CuisineId = @CuisineId,
    LocationId = @LocationId,
    FromDate = @FromDate,
    ToDate = @ToDate,
    Qty = @Qty,
    IsActive = @IsActive,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE RequestId = @RequestId;

SELECT @RequestId;";

                return await con.ExecuteScalarAsync<int>(updateSql, model);
            }
            else
            {
                const string insertSql = @"
INSERT INTO dbo.RequestMaster
(
    CompanyId,
    SessionId,
    CuisineId,
    LocationId,
    FromDate,
    ToDate,
    Qty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @CompanyId,
    @SessionId,
    @CuisineId,
    @LocationId,
    @FromDate,
    @ToDate,
    @Qty,
    @IsActive,
    @UserId,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await con.ExecuteScalarAsync<int>(insertSql, model);
            }
        }

        public async Task<bool> DeleteRequestAsync(int requestId, int? userId)
        {
            const string sql = @"
UPDATE dbo.RequestMaster
SET
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE RequestId = @RequestId;";

            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(sql, new { RequestId = requestId, UserId = userId });
            return rows > 0;
        }
    }
}
