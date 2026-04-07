using CateringApi.Data;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private readonly DapperContext _context;

        public ReportRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<ReportPageMasterDto> GetReportPageMastersAsync(int userId)
        {
            using var con = _context.CreateConnection();

            const string userSql = @"
SELECT 
    ISNULL(CompanyId, 0) AS CompanyId,
    ISNULL(RoleId, 0) AS RoleId
FROM dbo.UserMaster
WHERE Id = @UserId
  AND IsActive = 1;";

            var user = await con.QueryFirstOrDefaultAsync<dynamic>(userSql, new { UserId = userId });

            if (user == null)
            {
                return new ReportPageMasterDto();
            }

            int roleId = Convert.ToInt32(user.RoleId);
            int companyId = Convert.ToInt32(user.CompanyId);

            const string companySql = @"
SELECT
    c.Id,
    NULL AS CompanyId,
    c.CompanyName AS Name
FROM dbo.CompanyMaster c
WHERE c.IsActive = 1
  AND (@RoleId = 1 OR c.Id = @CompanyId)
ORDER BY c.CompanyName;";

            const string sessionSql = @"
SELECT
    s.Id,
    NULL AS CompanyId,
    s.SessionName AS Name
FROM dbo.Session s
WHERE s.IsActive = 1
ORDER BY s.Id;";

            const string cuisineSql = @"
SELECT
    c.Id,
    NULL AS CompanyId,
    c.CuisineName AS Name
FROM dbo.CuisineMaster c
WHERE c.IsActive = 1
ORDER BY c.Id;";

            const string locationSql = @"
SELECT
    l.Id,
    NULL AS CompanyId,
    l.LocationName AS Name
FROM dbo.Location l
WHERE l.IsActive = 1
ORDER BY l.Id;";

            const string companyNameSql = @"
SELECT CompanyName
FROM dbo.CompanyMaster
WHERE Id = @CompanyId;";

            var companies = await con.QueryAsync<DropdownDto>(companySql, new
            {
                RoleId = roleId,
                CompanyId = companyId
            });

            var sessions = await con.QueryAsync<DropdownDto>(sessionSql);
            var cuisines = await con.QueryAsync<DropdownDto>(cuisineSql);
            var locations = await con.QueryAsync<DropdownDto>(locationSql);

            string defaultCompanyName = string.Empty;
            if (companyId > 0)
            {
                defaultCompanyName = await con.QueryFirstOrDefaultAsync<string>(
                    companyNameSql,
                    new { CompanyId = companyId }
                ) ?? string.Empty;
            }

            return new ReportPageMasterDto
            {
                Companies = companies,
                Sessions = sessions,
                Cuisines = cuisines,
                Locations = locations,
                RoleId = roleId,
                DefaultCompanyId = companyId,
                DefaultCompanyName = defaultCompanyName
            };
        }

        public async Task<(IEnumerable<ReportByDateRowDto> Rows, IEnumerable<FoodTotalDto> Totals)>
    GetReportByDatesAsync(ReportFilterDto model)
        {
            using var con = _context.CreateConnection();

            const string userSql = @"
SELECT 
    ISNULL(CompanyId, 0) AS CompanyId,
    ISNULL(RoleId, 0) AS RoleId
FROM dbo.UserMaster
WHERE Id = @UserId
  AND IsActive = 1;";

            var user = await con.QueryFirstOrDefaultAsync<dynamic>(userSql, new { UserId = model.UserId });

            if (user == null)
                return (Enumerable.Empty<ReportByDateRowDto>(), Enumerable.Empty<FoodTotalDto>());

            int roleId = Convert.ToInt32(user.RoleId);
            int loggedInCompanyId = Convert.ToInt32(user.CompanyId);

            int? finalCompanyId = roleId == 2
                ? loggedInCompanyId
                : model.CompanyId;

            // 🔹 MAIN REPORT
            const string mainSql = @"
SELECT
    cm.CompanyName,
    rh.FromDate AS ReportDate,
    s.SessionName,
    cu.CuisineName,
    l.LocationName,
    SUM(rd.Qty) AS Count
FROM dbo.RequestHeader rh
INNER JOIN dbo.RequestDetail rd ON rd.RequestHeaderId = rh.Id AND rd.IsActive = 1
INNER JOIN dbo.CompanyMaster cm ON cm.Id = rh.CompanyId
INNER JOIN dbo.Session s ON s.Id = rd.SessionId
INNER JOIN dbo.CuisineMaster cu ON cu.Id = rd.CuisineId
INNER JOIN dbo.Location l ON l.Id = rd.LocationId
WHERE rh.IsActive = 1
  AND (@CompanyId IS NULL OR rh.CompanyId = @CompanyId)
  AND (@FromDate IS NULL OR rh.FromDate >= @FromDate)
  AND (@ToDate IS NULL OR rh.ToDate <= @ToDate)
  AND (@SessionId IS NULL OR rd.SessionId = @SessionId)
  AND (@CuisineId IS NULL OR rd.CuisineId = @CuisineId)
  AND (@LocationId IS NULL OR rd.LocationId = @LocationId)
GROUP BY
    cm.CompanyName,
    rh.FromDate,
    s.Id,
    s.SessionName,
    cu.Id,
    cu.CuisineName,
    l.Id,
    l.LocationName
ORDER BY
    rh.FromDate DESC,
    s.Id,
    cu.Id,
    l.Id;";

            // 🔥 FOOD TOTAL (NEW)
            const string totalSql = @"
SELECT
    cu.CuisineName,
    SUM(rd.Qty) AS TotalQty
FROM dbo.RequestHeader rh
INNER JOIN dbo.RequestDetail rd ON rd.RequestHeaderId = rh.Id AND rd.IsActive = 1
INNER JOIN dbo.CuisineMaster cu ON cu.Id = rd.CuisineId
WHERE rh.IsActive = 1
  AND (@CompanyId IS NULL OR rh.CompanyId = @CompanyId)
  AND (@FromDate IS NULL OR rh.FromDate >= @FromDate)
  AND (@ToDate IS NULL OR rh.ToDate <= @ToDate)
  AND (@SessionId IS NULL OR rd.SessionId = @SessionId)
  AND (@CuisineId IS NULL OR rd.CuisineId = @CuisineId)
  AND (@LocationId IS NULL OR rd.LocationId = @LocationId)
GROUP BY cu.CuisineName
ORDER BY cu.CuisineName;";

            var rows = await con.QueryAsync<ReportByDateRowDto>(mainSql, new
            {
                CompanyId = finalCompanyId,
                model.FromDate,
                model.ToDate,
                model.SessionId,
                model.CuisineId,
                model.LocationId
            });

            var totals = await con.QueryAsync<FoodTotalDto>(totalSql, new
            {
                CompanyId = finalCompanyId,
                model.FromDate,
                model.ToDate,
                model.SessionId,
                model.CuisineId,
                model.LocationId
            });

            return (rows, totals);
        }
    }
}