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

            const string companySql = @"
SELECT
    c.Id,
    NULL AS CompanyId,
    c.CompanyName AS Name
FROM dbo.CompanyMaster c
WHERE c.Id = @CompanyId
  AND c.IsActive = 1
ORDER BY c.Id;";

            const string sessionSql = @"
SELECT DISTINCT
    s.Id,
    csm.CompanyId,
    s.SessionName AS Name
FROM dbo.Session s
INNER JOIN dbo.CompanySessionMap csm ON csm.SessionId = s.Id
WHERE csm.CompanyId = @CompanyId
ORDER BY s.Id;";

            const string cuisineSql = @"
SELECT DISTINCT
    c.Id,
    ccm.CompanyId,
    c.CuisineName AS Name
FROM dbo.CuisineMaster c
INNER JOIN dbo.CompanyCuisineMap ccm ON ccm.CuisineId = c.Id
WHERE ccm.CompanyId = @CompanyId
  AND c.IsActive = 1
ORDER BY c.Id;";

            const string locationSql = @"
SELECT DISTINCT
    l.Id,
    clm.CompanyId,
    l.LocationName AS Name
FROM dbo.Location l
INNER JOIN dbo.CompanyLocationMap clm ON clm.LocationId = l.Id
WHERE clm.CompanyId = @CompanyId
  AND l.IsActive = 1
ORDER BY l.Id;";

            const string siteSettingsSql = @"
SELECT TOP 1
    OrderDays,
    BreakfastCutOffTime,
    LunchCutOffTime,
    LateLunchCutOffTime,
    DinnerCutOffTime,
    LateDinnerCutOffTime
FROM dbo.SiteSettings
WHERE IsActive = 1
ORDER BY Id DESC;";

            var companies = await con.QueryAsync<DropdownDto>(companySql, new { CompanyId = companyId ?? 0 });
            var sessions = await con.QueryAsync<DropdownDto>(sessionSql, new { CompanyId = companyId ?? 0 });
            var cuisines = await con.QueryAsync<DropdownDto>(cuisineSql, new { CompanyId = companyId ?? 0 });
            var locations = await con.QueryAsync<DropdownDto>(locationSql, new { CompanyId = companyId ?? 0 });

            var siteSettings = await con.QueryFirstOrDefaultAsync<SiteSettingsMasterDto>(siteSettingsSql);

            return new RequestPageMasterDto
            {
                Companies = companies,
                Sessions = sessions,
                Cuisines = cuisines,
                Locations = locations,
                OrderDays = siteSettings?.OrderDays ?? 3,

                BreakfastCutOffTime = siteSettings?.BreakfastCutOffTime,
                LunchCutOffTime = siteSettings?.LunchCutOffTime,
                LateLunchCutOffTime = siteSettings?.LateLunchCutOffTime,
                DinnerCutOffTime = siteSettings?.DinnerCutOffTime,
                LateDinnerCutOffTime = siteSettings?.LateDinnerCutOffTime
            };
        }

        public async Task<IEnumerable<RequestDto>> GetAllRequestsAsync(int userId)
        {
            using var con = _context.CreateConnection();

            const string userSql = @"
SELECT CompanyId, RoleId
FROM dbo.UserMaster
WHERE Id = @UserId
  AND IsActive = 1;";

            var user = await con.QueryFirstOrDefaultAsync<dynamic>(userSql, new { UserId = userId });

            if (user == null)
                return Enumerable.Empty<RequestDto>();

            int companyId = Convert.ToInt32(user.CompanyId ?? 0);
            int roleId = Convert.ToInt32(user.RoleId ?? 0);

            const string sql = @"
SELECT
    rh.Id,
    rh.RequestNo,
    rh.CompanyId,
    c.CompanyName,
    rh.FromDate,
    rh.ToDate,
    rh.TotalQty,
    rh.IsActive,
    rh.CreatedBy,
    rh.CreatedDate,
    rh.UpdatedBy,
    rh.UpdatedDate
FROM dbo.RequestHeader rh
INNER JOIN dbo.CompanyMaster c ON c.Id = rh.CompanyId
WHERE rh.IsActive = 1
  AND (
      (@RoleId IN (2, 4) AND rh.CompanyId = @CompanyId)
      OR (@RoleId NOT IN (2, 4))
    )
ORDER BY rh.Id DESC;";

            return await con.QueryAsync<RequestDto>(sql, new
            {
                CompanyId = companyId,
                RoleId = roleId
            });
        }

        public async Task<RequestDto?> GetRequestByIdAsync(int id)
        {
            using var con = _context.CreateConnection();

            const string headerSql = @"
SELECT
    rh.Id,
    rh.RequestNo,
    rh.CompanyId,
    c.CompanyName,
    rh.FromDate,
    rh.ToDate,
    rh.TotalQty,
    rh.IsActive,
    rh.CreatedBy,
    rh.CreatedDate,
    rh.UpdatedBy,
    rh.UpdatedDate
FROM dbo.RequestHeader rh
INNER JOIN dbo.CompanyMaster c ON c.Id = rh.CompanyId
WHERE rh.Id = @Id
  AND rh.IsActive = 1;";

            const string linesSql = @"
SELECT
    rd.Id,
    rd.RequestHeaderId,
    rd.PlanType,
    rd.CuisineId,
    cm.CuisineName,
    rd.Qty
FROM dbo.RequestDetail rd
INNER JOIN dbo.CuisineMaster cm ON cm.Id = rd.CuisineId
WHERE rd.RequestHeaderId = @Id
  AND rd.IsActive = 1
ORDER BY rd.Id;";

            var header = await con.QueryFirstOrDefaultAsync<RequestDto>(headerSql, new { Id = id });
            if (header == null)
                return null;

            var lines = await con.QueryAsync<RequestDetailDto>(linesSql, new { Id = id });
            header.Lines = lines.ToList();

            return header;
        }

        public async Task<int> SaveRequestAsync(RequestHeaderDto model)
        {
            using var con = _context.CreateConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                var totalQty = model.Lines?.Sum(x => x.Qty) ?? 0;

                const string overlapSql = @"
SELECT TOP 1 rh.RequestNo
FROM dbo.RequestHeader rh
WHERE rh.IsActive = 1
  AND rh.CompanyId = @CompanyId
  AND (@Id = 0 OR rh.Id <> @Id)
  AND rh.FromDate <= @ToDate
  AND rh.ToDate >= @FromDate
ORDER BY rh.Id DESC;";

                var overlappedRequestNo = await con.QueryFirstOrDefaultAsync<string>(
                    overlapSql,
                    new
                    {
                        CompanyId = model.CompanyId,
                        Id = model.Id ?? 0,
                        FromDate = model.FromDate,
                        ToDate = model.ToDate
                    },
                    tran
                );

                if (!string.IsNullOrWhiteSpace(overlappedRequestNo))
                {
                    throw new Exception($"Order already exists for the selected date range. Overlapping Order No: {overlappedRequestNo}");
                }


                if (model.Id.HasValue && model.Id.Value > 0)
                {
                    const string updateHeaderSql = @"
UPDATE dbo.RequestHeader
SET
    CompanyId = @CompanyId,
    FromDate = @FromDate,
    ToDate = @ToDate,
    TotalQty = @TotalQty,
    IsActive = @IsActive,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

                    await con.ExecuteAsync(updateHeaderSql, new
                    {
                        model.CompanyId,
                        model.FromDate,
                        model.ToDate,
                        TotalQty = totalQty,
                        model.IsActive,
                        model.UserId,
                        model.Id
                    }, tran);

                    const string deleteLinesSql = @"
DELETE FROM dbo.RequestDetail
WHERE RequestHeaderId = @RequestHeaderId;";

                    await con.ExecuteAsync(deleteLinesSql, new
                    {
                        RequestHeaderId = model.Id.Value
                    }, tran);

                    const string insertLineSql = @"
INSERT INTO dbo.RequestDetail
(
    RequestHeaderId,
    PlanType,
    CuisineId,
    Qty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @RequestHeaderId,
    @PlanType,
    @CuisineId,
    @Qty,
    1,
    @UserId,
    GETDATE()
);";
                    foreach (var line in model.Lines)
                    {
                        await con.ExecuteAsync(insertLineSql, new
                        {
                            RequestHeaderId = model.Id.Value,
                            line.PlanType,
                            line.CuisineId,
                            line.Qty,
                            model.UserId
                        }, tran);
                    }

                    tran.Commit();
                    return model.Id.Value;
                }
                else
                {
                    const string insertHeaderSql = @"
INSERT INTO dbo.RequestHeader
(
    RequestNo,
    CompanyId,
    FromDate,
    ToDate,
    TotalQty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    '',
    @CompanyId,
    @FromDate,
    @ToDate,
    @TotalQty,
    1,
    @UserId,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var newId = await con.ExecuteScalarAsync<int>(insertHeaderSql, new
                    {
                        model.CompanyId,
                        model.FromDate,
                        model.ToDate,
                        TotalQty = totalQty,
                        model.UserId
                    }, tran);

                    const string companyCodeSql = @"
SELECT CompanyCode
FROM dbo.CompanyMaster
WHERE Id = @CompanyId;";

                    var companyCode = await con.QueryFirstOrDefaultAsync<string>(
                        companyCodeSql,
                        new { CompanyId = model.CompanyId },
                        tran
                    );

                    companyCode = string.IsNullOrWhiteSpace(companyCode)
                        ? model.CompanyId.ToString()
                        : companyCode.Trim().ToUpper();

                    const string requestNoSql = @"
SELECT ISNULL(MAX(TRY_CONVERT(INT, RIGHT(RequestNo, 4))), 0) + 1
FROM dbo.RequestHeader WITH (UPDLOCK, HOLDLOCK)
WHERE CompanyId = @CompanyId
  AND RequestNo LIKE @Prefix + '%';";

                    var prefix = $"REQ-{companyCode}-";

                    var nextNo = await con.ExecuteScalarAsync<int>(
                        requestNoSql,
                        new { CompanyId = model.CompanyId, Prefix = prefix },
                        tran
                    );

                    var requestNo = $"{prefix}{nextNo.ToString("D4")}";

                    const string updateRequestNoSql = @"
UPDATE dbo.RequestHeader
SET RequestNo = @RequestNo
WHERE Id = @Id;";

                    await con.ExecuteAsync(updateRequestNoSql, new
                    {
                        RequestNo = requestNo,
                        Id = newId
                    }, tran);

                    const string insertLineSql = @"
INSERT INTO dbo.RequestDetail
(
    RequestHeaderId,
    PlanType,
    CuisineId,
    Qty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @RequestHeaderId,
    @PlanType,
    @CuisineId,
    @Qty,
    1,
    @UserId,
    GETDATE()
);";

                    foreach (var line in model.Lines)
                    {
                        await con.ExecuteAsync(insertLineSql, new
                        {
                            RequestHeaderId = newId,
                            line.PlanType,
                            line.CuisineId,
                            line.Qty,
                            model.UserId
                        }, tran);
                    }

                    tran.Commit();
                    return newId;
                }
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteRequestAsync(int id, int? userId)
        {
            using var con = _context.CreateConnection();
            con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                const string updateHeaderSql = @"
UPDATE dbo.RequestHeader
SET
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

                const string updateDetailSql = @"
UPDATE dbo.RequestDetail
SET
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE RequestHeaderId = @Id;";

                var rows = await con.ExecuteAsync(updateHeaderSql, new { Id = id, UserId = userId }, tran);
                await con.ExecuteAsync(updateDetailSql, new { Id = id, UserId = userId }, tran);

                tran.Commit();
                return rows > 0;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<int> GetOrderDays()
        {
            using var con = _context.CreateConnection();  

            const string siteSettingsSql = @"
SELECT OrderDays
FROM SiteSettings";
             
            var orderDays = await con.QueryFirstOrDefaultAsync<int?>(siteSettingsSql);

            return orderDays ?? 3;
        }

        public async Task<bool> CheckOverlapAsync(int companyId, DateTime fromDate, DateTime toDate, int id = 0)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT COUNT(1)
FROM dbo.RequestHeader rh
WHERE rh.IsActive = 1
  AND rh.CompanyId = @CompanyId
  AND (@Id = 0 OR rh.Id <> @Id)
  AND rh.FromDate <= @ToDate
  AND rh.ToDate >= @FromDate;";

            var count = await con.ExecuteScalarAsync<int>(sql, new
            {
                CompanyId = companyId,
                FromDate = fromDate,
                ToDate = toDate,
                Id = id
            });

            return count > 0;
        }




    }

}