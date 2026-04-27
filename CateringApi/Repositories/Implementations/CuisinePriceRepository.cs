using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class CuisinePriceRepository : ICuisinePriceRepository
    {
        private readonly DapperContext _context;
        private readonly FoodDBContext _context1;

        public CuisinePriceRepository(DapperContext context, FoodDBContext context1)
        {
            _context = context;
            _context1 = context1;
        }

        public async Task<IEnumerable<SessionDropdownDto>> GetAllSessionsAsync()
        {
            const string sql = @"
SELECT
    Id,
    SessionName
   
FROM dbo.[Session]
WHERE IsActive = 1
ORDER BY Id;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<SessionDropdownDto>(sql);
        }

        public async Task<bool> SaveDefaultPlanRatesBulkAsync(DefaultPlanBulkSaveRequest request)
        {
            using var con = _context.CreateConnection();

            if (con.State != ConnectionState.Open)
                con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                foreach (var plan in request.Plans.OrderBy(x => x.PlanType))
                {
                    foreach (var item in plan.SessionRates.OrderBy(x => x.SessionId))
                    {
                        const string existingSql = @"
SELECT TOP 1 Id, Rate, EffectiveFrom
FROM dbo.SessionPrice WITH (UPDLOCK, HOLDLOCK)
WHERE CompanyId = 0
  AND SessionId = @SessionId
  AND PlanType = @PlanType
  AND IsActive = 1
ORDER BY Id DESC;";

                        var existing = await con.QueryFirstOrDefaultAsync<SessionPriceExistingDto>(
                            existingSql,
                            new
                            {
                                SessionId = item.SessionId,
                                PlanType = plan.PlanType
                            },
                            tran
                        );

                        if (existing != null)
                        {
                            if (existing.Rate == item.Rate &&
                                existing.EffectiveFrom.HasValue &&
                                existing.EffectiveFrom.Value.Date == plan.EffectiveFrom.Date)
                            {
                                continue;
                            }

                            const string closeHistorySql = @"
UPDATE dbo.SessionPriceHistory
SET EffectiveTo = DATEADD(DAY, -1, @EffectiveFrom)
WHERE PriceId = @PriceId
  AND EffectiveTo IS NULL;";

                            await con.ExecuteAsync(
                                closeHistorySql,
                                new
                                {
                                    EffectiveFrom = plan.EffectiveFrom,
                                    PriceId = existing.Id
                                },
                                tran
                            );

                            const string updateSql = @"
UPDATE dbo.SessionPrice
SET Rate = @Rate,
    EffectiveFrom = @EffectiveFrom,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

                            await con.ExecuteAsync(
                                updateSql,
                                new
                                {
                                    Id = existing.Id,
                                    Rate = item.Rate,
                                    EffectiveFrom = plan.EffectiveFrom,
                                    UpdatedBy = request.UpdatedBy
                                },
                                tran
                            );

                            const string insertHistorySql = @"
INSERT INTO dbo.SessionPriceHistory
(
    PriceId,
    CompanyId,
    SessionId,
    PlanType,
    Rate,
    EffectiveFrom,
    EffectiveTo,
    ActionType,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @PriceId,
    0,
    @SessionId,
    @PlanType,
    @Rate,
    @EffectiveFrom,
    NULL,
    'UPDATE',
    @CreatedBy,
    GETDATE()
);";

                            await con.ExecuteAsync(
                                insertHistorySql,
                                new
                                {
                                    PriceId = existing.Id,
                                    SessionId = item.SessionId,
                                    PlanType = plan.PlanType,
                                    Rate = item.Rate,
                                    EffectiveFrom = plan.EffectiveFrom,
                                    CreatedBy = request.UpdatedBy
                                },
                                tran
                            );
                        }
                        else
                        {
                            const string insertSql = @"
INSERT INTO dbo.SessionPrice
(
    CompanyId,
    SessionId,
    PlanType,
    Rate,
    EffectiveFrom,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    0,
    @SessionId,
    @PlanType,
    @Rate,
    @EffectiveFrom,
    1,
    @CreatedBy,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            var newId = await con.ExecuteScalarAsync<int>(
                                insertSql,
                                new
                                {
                                    SessionId = item.SessionId,
                                    PlanType = plan.PlanType,
                                    Rate = item.Rate,
                                    EffectiveFrom = plan.EffectiveFrom,
                                    CreatedBy = request.UpdatedBy
                                },
                                tran
                            );

                            const string insertHistorySql = @"
INSERT INTO dbo.SessionPriceHistory
(
    PriceId,
    CompanyId,
    SessionId,
    PlanType,
    Rate,
    EffectiveFrom,
    EffectiveTo,
    ActionType,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @PriceId,
    0,
    @SessionId,
    @PlanType,
    @Rate,
    @EffectiveFrom,
    NULL,
    'INSERT',
    @CreatedBy,
    GETDATE()
);";

                            await con.ExecuteAsync(
                                insertHistorySql,
                                new
                                {
                                    PriceId = newId,
                                    SessionId = item.SessionId,
                                    PlanType = plan.PlanType,
                                    Rate = item.Rate,
                                    EffectiveFrom = plan.EffectiveFrom,
                                    CreatedBy = request.UpdatedBy
                                },
                                tran
                            );
                        }
                    }
                }

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<List<CompanyPlanRateViewDto>> GetDefaultPlanRatesAsync()
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    sp.PlanType,
    sp.EffectiveFrom,
    sp.SessionId,
    s.SessionName,
    sp.Rate
FROM dbo.SessionPrice sp
INNER JOIN dbo.[Session] s ON s.Id = sp.SessionId
WHERE sp.CompanyId = 0
  AND sp.IsActive = 1
ORDER BY
    CASE sp.PlanType
        WHEN 'Basic' THEN 1
        WHEN 'Standard' THEN 2
        WHEN 'Premium' THEN 3
        ELSE 4
    END,
    sp.SessionId;";

            var rows = await con.QueryAsync(sql);

            var result = rows
                .GroupBy(x => (string)x.PlanType)
                .Select(g => new CompanyPlanRateViewDto
                {
                    PlanType = g.Key,
                    EffectiveFrom = g.Max(x => (DateTime?)x.EffectiveFrom),
                    SessionRates = g.Select(x => new PlanSessionRateViewDto
                    {
                        SessionId = (int)x.SessionId,
                        SessionName = (string)x.SessionName,
                        Rate = (decimal)x.Rate
                    }).ToList()
                })
                .ToList();

            return result;
        }

        public async Task<IEnumerable<CuisinePriceHistoryDto>> GetDefaultPriceHistoryAsync(int sessionId, string planType)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    h.Id,
    h.PriceId,
    h.CompanyId,
    h.SessionId,
    0 AS CuisineId,
    '' AS CuisineName,
    h.Rate,
    h.EffectiveFrom,
    h.EffectiveTo,
    h.ActionType,
    h.CreatedBy,
    h.CreatedDate
FROM dbo.SessionPriceHistory h
WHERE h.CompanyId = 0
  AND h.SessionId = @SessionId
  AND h.PlanType = @PlanType
ORDER BY h.EffectiveFrom DESC, h.Id DESC;";

            return await con.QueryAsync<CuisinePriceHistoryDto>(sql, new
            {
                SessionId = sessionId,
                PlanType = planType
            });
        }

        public async Task<List<PriceListDto>> GetPriceList()
        {
            var baseQuery = from p in _context1.SessionPrice
                            join s in _context1.Session on p.SessionId equals s.Id
                            where p.IsActive
                                  && p.CompanyId == 0
                            select new
                            {
                                p.Id,
                                p.CompanyId,
                                p.SessionId,
                                SessionName = s.SessionName,
                                p.Rate,
                                p.EffectiveFrom,
                                p.PlanType
                            };

            var rawData = await baseQuery
                .OrderBy(x => x.PlanType)
                .ThenBy(x => x.SessionName)
                .ThenByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            var result = rawData
                .GroupBy(x => new
                {
                    PlanType = (x.PlanType ?? "").Trim().ToLower(),
                    x.SessionId
                })
                .Select(g => g
                    .OrderByDescending(x => x.EffectiveFrom)
                    .ThenByDescending(x => x.Id)
                    .First())
                .Select(x => new PriceListDto
                {
                    Id = x.Id,
                    PriceId = x.Id,
                    CompanyId = x.CompanyId,
                    CompanyName = "Default For All Companies",
                    SessionId = x.SessionId,
                    SessionName = x.SessionName,
                    CuisineId = 0,
                    CuisineName = string.Empty,
                    Rate = x.Rate,
                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = null,
                    ActionType = x.EffectiveFrom.Date > DateTime.Today ? "FUTURE" : "DEFAULT",
                    IsActive = true,
                    IsCurrent = x.EffectiveFrom.Date <= DateTime.Today,
                    PlanType = x.PlanType
                })
                .OrderBy(x => x.PlanType)
                .ThenBy(x => x.SessionName)
                .ToList();

            return result;
        }

        public class SessionPriceExistingDto
        {
            public int Id { get; set; }
            public decimal Rate { get; set; }
            public DateTime? EffectiveFrom { get; set; }
        }
    }
}