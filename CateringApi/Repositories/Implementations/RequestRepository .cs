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

            var companies = await con.QueryAsync<DropdownDto>(companySql, new { CompanyId = companyId ?? 0 });
            var sessions = await con.QueryAsync<DropdownDto>(sessionSql, new { CompanyId = companyId ?? 0 });
            var cuisines = await con.QueryAsync<DropdownDto>(cuisineSql, new { CompanyId = companyId ?? 0 });
            var locations = await con.QueryAsync<DropdownDto>(locationSql, new { CompanyId = companyId ?? 0 });

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
  AND rh.CompanyId = @CompanyId
ORDER BY rh.Id DESC;";

            return await con.QueryAsync<RequestDto>(sql, new { CompanyId = companyId ?? 0 });
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
    rd.SessionId,
    s.SessionName,
    rd.CuisineId,
    cm.CuisineName,
    rd.LocationId,
    l.LocationName,
    rd.Qty
FROM dbo.RequestDetail rd
INNER JOIN dbo.Session s ON s.Id = rd.SessionId
INNER JOIN dbo.CuisineMaster cm ON cm.Id = rd.CuisineId
INNER JOIN dbo.Location l ON l.Id = rd.LocationId
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
    SessionId,
    CuisineId,
    LocationId,
    Qty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @RequestHeaderId,
    @SessionId,
    @CuisineId,
    @LocationId,
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
                            line.SessionId,
                            line.CuisineId,
                            line.LocationId,
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

                    var requestNo = $"REQ-{newId.ToString("D5")}";

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
    SessionId,
    CuisineId,
    LocationId,
    Qty,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @RequestHeaderId,
    @SessionId,
    @CuisineId,
    @LocationId,
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
                            line.SessionId,
                            line.CuisineId,
                            line.LocationId,
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
    }
}