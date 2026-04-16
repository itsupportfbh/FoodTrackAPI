using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class CuisinePriceRepository : ICuisinePriceRepository
    {
        private readonly DapperContext _context;

        public CuisinePriceRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CuisineRateViewModel>> GetAllCuisinesWithRatesAsync(int companyId, int sessionId)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    c.Id AS CuisineId,
    c.CuisineName,
    ISNULL(cp.Rate, 0) AS Rate,
    cp.EffectiveFrom
FROM dbo.CuisineMaster c
LEFT JOIN dbo.CuisinePrice cp
    ON cp.CuisineId = c.Id
   AND cp.CompanyId = @CompanyId
   AND cp.SessionId = @SessionId
   AND cp.IsActive = 1
WHERE c.IsActive = 1
ORDER BY c.CuisineName;";

            return await con.QueryAsync<CuisineRateViewModel>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId
            });
        }

        public async Task<IEnumerable<CuisineRateViewModel>> GetCuisineRatesByCompanySessionAsync(int companyId, int sessionId)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    cp.CuisineId,
    c.CuisineName,
    cp.Rate,
    cp.EffectiveFrom
FROM dbo.CuisinePrice cp
INNER JOIN dbo.CuisineMaster c ON c.Id = cp.CuisineId
WHERE cp.CompanyId = @CompanyId
  AND cp.SessionId = @SessionId
  AND cp.IsActive = 1
ORDER BY c.CuisineName;";

            return await con.QueryAsync<CuisineRateViewModel>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId
            });
        }

        public async Task<bool> SaveBulkCuisinePricesAsync(BulkCuisinePriceSaveRequest request)
        {
            using var con = _context.CreateConnection();

            if (con.State != ConnectionState.Open)
                con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                foreach (var item in request.Rates)
                {
                    const string checkSql = @"
SELECT Id, Rate, EffectiveFrom
FROM dbo.CuisinePrice
WHERE CompanyId = @CompanyId
  AND SessionId = @SessionId
  AND CuisineId = @CuisineId
  AND IsActive = 1;";

                    var existing = await con.QueryFirstOrDefaultAsync<CuisinePriceExistingDto>(
                        checkSql,
                        new
                        {
                            request.CompanyId,
                            request.SessionId,
                            item.CuisineId
                        },
                        tran);

                    if (existing != null)
                    {
                        if (existing.Rate == item.Rate &&
                            existing.EffectiveFrom.HasValue &&
                            existing.EffectiveFrom.Value.Date == item.EffectiveFrom.Date)
                            continue;

                        const string closeHistorySql = @"
UPDATE dbo.CuisinePriceHistory
SET EffectiveTo = DATEADD(SECOND, -1, @NewEffectiveFrom)
WHERE PriceId = @PriceId
  AND EffectiveTo IS NULL;";

                        await con.ExecuteAsync(
                            closeHistorySql,
                            new
                            {
                                PriceId = existing.Id,
                                NewEffectiveFrom = item.EffectiveFrom
                            },
                            tran);

                        const string updateSql = @"
UPDATE dbo.CuisinePrice
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
                                EffectiveFrom = item.EffectiveFrom,
                                UpdatedBy = request.UpdatedBy
                            },
                            tran);

                        const string insertHistorySql = @"
INSERT INTO dbo.CuisinePriceHistory
(
    PriceId,
    CompanyId,
    SessionId,
    CuisineId,
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
    @CuisineId,
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
                                item.CuisineId,
                                Rate = item.Rate,
                                EffectiveFrom = item.EffectiveFrom,
                                CreatedBy = request.UpdatedBy
                            },
                            tran);
                    }
                    else
                    {
                        const string insertSql = @"
INSERT INTO dbo.CuisinePrice
(
    CompanyId,
    SessionId,
    CuisineId,
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
    @CuisineId,
    @Rate,
    @EffectiveFrom,
    1,
    @CreatedBy,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        var newPriceId = await con.ExecuteScalarAsync<int>(
                            insertSql,
                            new
                            {
                                request.CompanyId,
                                request.SessionId,
                                item.CuisineId,
                                Rate = item.Rate,
                                EffectiveFrom = item.EffectiveFrom,
                                CreatedBy = request.UpdatedBy
                            },
                            tran);

                        const string insertHistorySql = @"
INSERT INTO dbo.CuisinePriceHistory
(
    PriceId,
    CompanyId,
    SessionId,
    CuisineId,
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
    @CuisineId,
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
                                PriceId = newPriceId,
                                request.CompanyId,
                                request.SessionId,
                                item.CuisineId,
                                Rate = item.Rate,
                                EffectiveFrom = item.EffectiveFrom,
                                CreatedBy = request.UpdatedBy
                            },
                            tran);
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

        public async Task<IEnumerable<CuisinePriceHistoryDto>> GetCuisinePriceHistoryAsync(int companyId, int sessionId, int cuisineId)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    h.Id,
    h.PriceId,
    h.CompanyId,
    h.SessionId,
    h.CuisineId,
    c.CuisineName,
    h.Rate,
    h.EffectiveFrom,
    h.EffectiveTo,
    h.ActionType,
    h.CreatedBy,
    h.CreatedDate
FROM dbo.CuisinePriceHistory h
INNER JOIN dbo.CuisineMaster c ON c.Id = h.CuisineId
WHERE h.CompanyId = @CompanyId
  AND h.SessionId = @SessionId
  AND h.CuisineId = @CuisineId
ORDER BY h.EffectiveFrom DESC, h.Id DESC;";

            return await con.QueryAsync<CuisinePriceHistoryDto>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId,
                CuisineId = cuisineId
            });
        }

        public async Task<decimal> GetApplicableCuisineRateAsync(int companyId, int sessionId, int cuisineId, DateTime orderDate)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT TOP 1 h.Rate
FROM dbo.CuisinePriceHistory h
WHERE h.CompanyId = @CompanyId
  AND h.SessionId = @SessionId
  AND h.CuisineId = @CuisineId
  AND h.EffectiveFrom <= @OrderDate
  AND (h.EffectiveTo IS NULL OR h.EffectiveTo >= @OrderDate)
ORDER BY h.EffectiveFrom DESC, h.Id DESC;";

            return await con.QueryFirstOrDefaultAsync<decimal>(sql, new
            {
                CompanyId = companyId,
                SessionId = sessionId,
                CuisineId = cuisineId,
                OrderDate = orderDate
            });
        }
    }

    public class CuisinePriceExistingDto
    {
        public int Id { get; set; }
        public decimal Rate { get; set; }
        public DateTime? EffectiveFrom { get; set; }
    }
}