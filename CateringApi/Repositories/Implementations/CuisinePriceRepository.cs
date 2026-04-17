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

        #region Session Based Methods

        public async Task<IEnumerable<CuisineRateViewModel>> GetAllCuisinesWithRatesAsync(int companyId, int sessionId)
        {
            using var con = _context.CreateConnection();

            // Since price is now session based only, same session rate will be shown for all cuisines
            const string sql = @"
SELECT
    c.Id AS CuisineId,
    c.CuisineName,
    ISNULL(sp.Rate, 0) AS Rate,
    sp.EffectiveFrom
FROM dbo.CompanyCuisineMap ccm
INNER JOIN dbo.CuisineMaster c
    ON c.Id = ccm.CuisineId
   AND c.IsActive = 1
LEFT JOIN dbo.SessionPrice sp
    ON sp.CompanyId = ccm.CompanyId
   AND sp.SessionId = @SessionId
   AND sp.IsActive = 1
WHERE ccm.CompanyId = @CompanyId
ORDER BY c.CuisineName;";

            return await con.QueryAsync<CuisineRateViewModel>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId
            });
        }

        public async Task<SessionRateViewDto?> GetSessionRateAsync(int companyId, int sessionId)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT TOP 1
    sp.Id,
    sp.CompanyId,
    sp.SessionId,
    cm.CompanyName,
    s.SessionName,
    sp.Rate,
    sp.EffectiveFrom
FROM dbo.SessionPrice sp
INNER JOIN dbo.CompanyMaster cm
    ON cm.Id = sp.CompanyId
INNER JOIN dbo.Session s
    ON s.Id = sp.SessionId
WHERE sp.CompanyId = @CompanyId
  AND sp.SessionId = @SessionId
  AND sp.IsActive = 1
ORDER BY sp.EffectiveFrom DESC, sp.Id DESC;";

            return await con.QueryFirstOrDefaultAsync<SessionRateViewDto>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId
            });
        }

        public async Task<bool> SaveSessionRateAsync(SessionRateSaveRequest request)
        {
            using var con = _context.CreateConnection();

            if (con.State != ConnectionState.Open)
                con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                const string existingSql = @"
SELECT TOP 1 Id, Rate, EffectiveFrom
FROM dbo.SessionPrice
WHERE CompanyId = @CompanyId
  AND SessionId = @SessionId
  AND IsActive = 1
ORDER BY Id DESC;";

                var existing = await con.QueryFirstOrDefaultAsync<SessionPriceExistingDto>(
                    existingSql,
                    new
                    {
                        request.CompanyId,
                        request.SessionId
                    },
                    tran
                );

                if (existing != null)
                {
                    if (existing.Rate == request.Rate &&
                        existing.EffectiveFrom.HasValue &&
                        existing.EffectiveFrom.Value.Date == request.EffectiveFrom.Date)
                    {
                        tran.Commit();
                        return true;
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
                            EffectiveFrom = request.EffectiveFrom,
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
                            request.Rate,
                            request.EffectiveFrom,
                            request.UpdatedBy
                        },
                        tran
                    );

                    const string insertHistorySql = @"
INSERT INTO dbo.SessionPriceHistory
(
    PriceId,
    CompanyId,
    SessionId,
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
    @CompanyId,
    @SessionId,
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
                            request.CompanyId,
                            request.SessionId,
                            request.Rate,
                            request.EffectiveFrom,
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
    Rate,
    EffectiveFrom,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @CompanyId,
    @SessionId,
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
                            request.CompanyId,
                            request.SessionId,
                            request.Rate,
                            request.EffectiveFrom,
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
    @CompanyId,
    @SessionId,
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
                            request.CompanyId,
                            request.SessionId,
                            request.Rate,
                            request.EffectiveFrom,
                            CreatedBy = request.UpdatedBy
                        },
                        tran
                    );
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

        public async Task<IEnumerable<CuisinePriceHistoryDto>> GetCuisinePriceHistoryAsync(int companyId, int sessionId, int cuisineId)
        {
            using var con = _context.CreateConnection();

            // Since history is now session based only, cuisineId is ignored.
            // Returning session history and mapping Cuisine columns with default values.
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
WHERE h.CompanyId = @CompanyId
  AND h.SessionId = @SessionId
ORDER BY h.EffectiveFrom DESC, h.Id DESC;";

            return await con.QueryAsync<CuisinePriceHistoryDto>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId
            });
        }

        public async Task<decimal> GetApplicableCuisineRateAsync(int companyId, int sessionId, int cuisineId, DateTime orderDate)
        {
            using var con = _context.CreateConnection();

            // Cuisine is no longer used. Same session rate applies.
            const string sql = @"
SELECT TOP 1 h.Rate
FROM dbo.SessionPriceHistory h
WHERE h.CompanyId = @CompanyId
  AND h.SessionId = @SessionId
  AND h.EffectiveFrom <= @OrderDate
  AND (h.EffectiveTo IS NULL OR h.EffectiveTo >= @OrderDate)
ORDER BY h.EffectiveFrom DESC, h.Id DESC;";

            return await con.QueryFirstOrDefaultAsync<decimal>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId,
                OrderDate = orderDate
            });
        }

        public async Task<List<PriceListDto>> GetPriceList()
        {
            var query = from h in _context1.SessionPriceHistory
                        join c in _context1.CompanyMaster on h.CompanyId equals c.Id
                        join s in _context1.Session on h.SessionId equals s.Id
                        join p in _context1.SessionPrice on h.PriceId equals p.Id into priceJoin
                        from p in priceJoin.DefaultIfEmpty()
                        orderby h.CreatedDate descending
                        select new PriceListDto
                        {
                            Id = h.Id,
                            PriceId = h.PriceId,
                            CompanyId = h.CompanyId,
                            CompanyName = c.CompanyName,
                            SessionId = h.SessionId,
                            SessionName = s.SessionName,
                            CuisineId = 0,
                            CuisineName = string.Empty,
                            Rate = h.Rate,
                            EffectiveFrom = h.EffectiveFrom,
                            EffectiveTo = h.EffectiveTo,
                            ActionType = h.ActionType,
                            IsActive = p != null && p.IsActive,
                            IsCurrent = p != null && p.IsActive
                        };

            return await query.ToListAsync();
        }

        #endregion
    }

    public class SessionPriceExistingDto
    {
        public int Id { get; set; }
        public decimal Rate { get; set; }
        public DateTime? EffectiveFrom { get; set; }
    }
}