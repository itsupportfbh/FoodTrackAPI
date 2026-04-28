using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Dashboard;
using CateringApi.DTOs.QR;
using CateringApi.DTOs.Session;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;

namespace CateringApi.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly FoodDBContext _context;

        public DashboardRepository(FoodDBContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> GetDashboardData(DashboardFilterDTO filter)
        {
            var con = _context.Database.GetDbConnection();

            if (con.State != ConnectionState.Open)
                await con.OpenAsync();

            var today = DateTime.Today;
            var fromDate = filter.FromDate?.Date ?? today;
            var toDate = filter.ToDate?.Date ?? today;

            var companyIds = filter.CompanyIds ?? new List<int>();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", fromDate);
            parameters.Add("@ToDate", toDate);
            parameters.Add("@Today", today);
            parameters.Add("@CompanyIds", companyIds);

            var companyFilter = companyIds.Any()
                ? " AND rh.CompanyId IN @CompanyIds "
                : "";

            var scanCompanyFilter = companyIds.Any()
                ? " AND qcr.CompanyId IN @CompanyIds "
                : "";

            var sql = $@"
IF OBJECT_ID('tempdb..#FinalPerDay') IS NOT NULL DROP TABLE #FinalPerDay;

CREATE TABLE #FinalPerDay
(
    RequestHeaderId INT,
    CompanyId INT,
    CompanyName NVARCHAR(250),
    PlanType NVARCHAR(100),
    OrderDate DATE,
    Qty DECIMAL(18,2),
    PerDayRate DECIMAL(18,2),
    TotalPrice DECIMAL(18,2)
);

;WITH DateRange AS
(
    SELECT @FromDate AS OrderDate
    UNION ALL
    SELECT DATEADD(DAY, 1, OrderDate)
    FROM DateRange
    WHERE OrderDate < @ToDate
),
ActiveHeaders AS
(
    SELECT
        rh.Id,
        rh.CompanyId,
        cm.CompanyName,
        rh.FromDate,
        rh.ToDate
    FROM RequestHeader rh
    LEFT JOIN CompanyMaster cm
        ON cm.Id = rh.CompanyId
    WHERE
        rh.IsActive = 1
        AND rh.FromDate <= @ToDate
        AND rh.ToDate >= @FromDate
        {companyFilter}
),
LatestOverride AS
(
    SELECT
        x.Id,
        x.RequestHeaderId,
        x.FromDate,
        x.ToDate
    FROM
    (
        SELECT
            ro.Id,
            ro.RequestHeaderId,
            ro.FromDate,
            ro.ToDate,
            ROW_NUMBER() OVER
            (
                PARTITION BY ro.RequestHeaderId, ro.FromDate, ro.ToDate
                ORDER BY ro.CreatedDate DESC, ro.Id DESC
            ) AS rn
        FROM RequestOverride ro
        INNER JOIN ActiveHeaders ah
            ON ah.Id = ro.RequestHeaderId
        WHERE
            ro.IsActive = 1
            AND ro.FromDate <= @ToDate
            AND ro.ToDate >= @FromDate
    ) x
    WHERE x.rn = 1
),
BasePerDay AS
(
    SELECT
        ah.Id AS RequestHeaderId,
        ah.CompanyId,
        ah.CompanyName,
        ISNULL(NULLIF(LTRIM(RTRIM(rd.PlanType)), ''), 'Basic') AS PlanType,
        dr.OrderDate,
        ISNULL(rd.Qty, 0) AS Qty
    FROM ActiveHeaders ah
    INNER JOIN RequestDetail rd
        ON rd.RequestHeaderId = ah.Id
    INNER JOIN DateRange dr
        ON dr.OrderDate BETWEEN ah.FromDate AND ah.ToDate
    WHERE
        rd.IsActive = 1
        AND NOT EXISTS
        (
            SELECT 1
            FROM RequestOverride ro
            WHERE ro.IsActive = 1
              AND ro.RequestHeaderId = ah.Id
              AND dr.OrderDate BETWEEN ro.FromDate AND ro.ToDate
        )
),
OverridePerDay AS
(
    SELECT
        ah.Id AS RequestHeaderId,
        ah.CompanyId,
        ah.CompanyName,
        ISNULL(NULLIF(LTRIM(RTRIM(rod.PlanType)), ''), 'Basic') AS PlanType,
        dr.OrderDate,
        ISNULL(rod.OverrideQty, 0) AS Qty
    FROM LatestOverride lo
    INNER JOIN ActiveHeaders ah
        ON ah.Id = lo.RequestHeaderId
    INNER JOIN RequestOverrideDetail rod
        ON rod.RequestOverrideId = lo.Id
    INNER JOIN DateRange dr
        ON dr.OrderDate BETWEEN lo.FromDate AND lo.ToDate
    WHERE
        rod.IsActive = 1
        AND ISNULL(rod.IsCancelled, 0) = 0
)
INSERT INTO #FinalPerDay
(
    RequestHeaderId,
    CompanyId,
    CompanyName,
    PlanType,
    OrderDate,
    Qty,
    PerDayRate,
    TotalPrice
)
SELECT
    x.RequestHeaderId,
    x.CompanyId,
    x.CompanyName,
    x.PlanType,
    x.OrderDate,
    x.Qty,
    ISNULL(price.PerDayRate, 0) AS PerDayRate,
    x.Qty * ISNULL(price.PerDayRate, 0) AS TotalPrice
FROM
(
    SELECT * FROM BasePerDay
    UNION ALL
    SELECT * FROM OverridePerDay
) x
OUTER APPLY
(
    SELECT
        SUM(ISNULL(ph.Rate, 0)) AS PerDayRate
    FROM
    (
        SELECT
            p.SessionId,
            p.Rate
        FROM
        (
            SELECT
                sph.SessionId,
                sph.Rate,
                ROW_NUMBER() OVER
                (
                    PARTITION BY sph.SessionId
                    ORDER BY sph.EffectiveFrom DESC, sph.Id DESC
                ) AS rn
            FROM SessionPriceHistory sph
            WHERE
                LTRIM(RTRIM(sph.PlanType)) = LTRIM(RTRIM(x.PlanType))
                AND sph.SessionId IN (1, 2, 3)
                AND ISNULL(sph.ActionType, '') <> 'DELETE'
                AND CAST(sph.EffectiveFrom AS DATE) <= x.OrderDate
                AND (
                    sph.EffectiveTo IS NULL
                    OR CAST(sph.EffectiveTo AS DATE) >= x.OrderDate
                )
        ) p
        WHERE p.rn = 1
    ) ph
) price
WHERE x.Qty > 0
OPTION (MAXRECURSION 32767);

SELECT
    COUNT(DISTINCT f.CompanyId) AS TotalCompanies,
    COUNT(DISTINCT f.RequestHeaderId) AS TotalOrders,
    ISNULL(SUM(CASE WHEN f.OrderDate = @Today THEN f.Qty ELSE 0 END), 0) AS TodayOrderedQty,
    0 AS TodayRedeemedQty,
    ISNULL(SUM(CASE WHEN f.OrderDate = @Today THEN f.Qty ELSE 0 END), 0) AS TodayPendingQty,
    ISNULL(SUM(f.Qty), 0) AS MonthOrderedQty,
    0 AS MonthRedeemedQty,
    ISNULL(SUM(f.Qty), 0) AS MonthPendingQty,
    ISNULL(SUM(f.TotalPrice), 0) AS TotalPrice
FROM #FinalPerDay f;

SELECT
    f.PlanType,
    ISNULL(SUM(f.Qty), 0) AS TotalQty
FROM #FinalPerDay f
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    0 AS SessionId,
    f.PlanType AS SessionName,
    ISNULL(SUM(f.Qty), 0) AS TotalQty
FROM #FinalPerDay f
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    f.PlanType,
    0 AS SessionId,
    f.PlanType AS SessionName,
    ISNULL(SUM(f.Qty), 0) AS Qty,
    CASE 
        WHEN ISNULL(SUM(f.Qty), 0) = 0 THEN 0
        ELSE ISNULL(SUM(f.TotalPrice), 0) / NULLIF(SUM(f.Qty), 0)
    END AS Rate,
    ISNULL(SUM(f.TotalPrice), 0) AS TotalPrice
FROM #FinalPerDay f
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    cp.PlanType,
    0 AS SessionId,
    cp.PlanType AS SessionName,
    cp.PerDayRate AS Rate
FROM
(
    SELECT
        p.PlanType,
        SUM(p.Rate) AS PerDayRate
    FROM
    (
        SELECT
            sph.PlanType,
            sph.SessionId,
            sph.Rate,
            ROW_NUMBER() OVER
            (
                PARTITION BY sph.PlanType, sph.SessionId
                ORDER BY sph.EffectiveFrom DESC, sph.Id DESC
            ) AS rn
        FROM SessionPriceHistory sph
        WHERE
            sph.SessionId IN (1, 2, 3)
            AND ISNULL(sph.ActionType, '') <> 'DELETE'
            AND CAST(sph.EffectiveFrom AS DATE) <= @ToDate
            AND (
                sph.EffectiveTo IS NULL
                OR CAST(sph.EffectiveTo AS DATE) >= @ToDate
            )
    ) p
    WHERE p.rn = 1
    GROUP BY p.PlanType
) cp
ORDER BY cp.PlanType;

SELECT
    f.CompanyId,
    ISNULL(f.CompanyName, 'Unknown Company') AS CompanyName,
    ISNULL(SUM(f.Qty), 0) AS TotalQty,
    0 AS RedeemQty,
    ISNULL(SUM(f.Qty), 0) AS PendingQty
FROM #FinalPerDay f
GROUP BY f.CompanyId, f.CompanyName
ORDER BY TotalQty DESC;

DROP TABLE #FinalPerDay;
";

            using var multi = await con.QueryMultipleAsync(sql, parameters);

            var summary = await multi.ReadFirstOrDefaultAsync<DashboardDTO>() ?? new DashboardDTO();

            summary.TotalOrdersByPlanType =
                (await multi.ReadAsync<PlanTypeQtyDTO>()).ToList();

            summary.TotalOrdersBySession =
                (await multi.ReadAsync<SessionQtyDTO>()).ToList();

            summary.SessionPriceBreakdown =
                (await multi.ReadAsync<SessionPriceBreakdownDTO>()).ToList();

            summary.CurrentSessionPrices =
                (await multi.ReadAsync<CurrentSessionPriceDTO>()).ToList();

            summary.TotalcompanyWiseOrders =
                (await multi.ReadAsync<CompanyWiseOrderDTO>()).ToList();

            summary.TotalPrice = summary.SessionPriceBreakdown.Sum(x => x.TotalPrice);

            summary.TodayScans = await con.ExecuteScalarAsync<int>($@"
SELECT COUNT(1)
FROM QrScanLog qsl
LEFT JOIN QrCodeRequest qcr
    ON qcr.Id = qsl.QrCodeRequestId
WHERE
    ISNULL(qsl.IsAllowed, 0) = 1
    AND CAST(qsl.ScanDate AS DATE) = @Today
    {scanCompanyFilter};
", parameters);

            summary.YesterdayScans = await con.ExecuteScalarAsync<int>($@"
SELECT COUNT(1)
FROM QrScanLog qsl
LEFT JOIN QrCodeRequest qcr
    ON qcr.Id = qsl.QrCodeRequestId
WHERE
    ISNULL(qsl.IsAllowed, 0) = 1
    AND CAST(qsl.ScanDate AS DATE) = DATEADD(DAY, -1, @Today)
    {scanCompanyFilter};
", parameters);

            summary.MonthRedeemedQty = await con.ExecuteScalarAsync<int>($@"
SELECT COUNT(1)
FROM QrScanLog qsl
LEFT JOIN QrCodeRequest qcr
    ON qcr.Id = qsl.QrCodeRequestId
WHERE
    ISNULL(qsl.IsAllowed, 0) = 1
    AND CAST(qsl.ScanDate AS DATE) BETWEEN @FromDate AND @ToDate
    {scanCompanyFilter};
", parameters);

            summary.TodayRedeemedQty = summary.TodayScans;
            summary.MonthPendingQty = Math.Max(0, summary.MonthOrderedQty - summary.MonthRedeemedQty);
            summary.TodayPendingQty = Math.Max(0, summary.TodayOrderedQty - summary.TodayRedeemedQty);

            foreach (var company in summary.TotalcompanyWiseOrders)
            {
                company.RedeemQty = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM QrScanLog qsl
LEFT JOIN QrCodeRequest qcr
    ON qcr.Id = qsl.QrCodeRequestId
WHERE
    ISNULL(qsl.IsAllowed, 0) = 1
    AND qcr.CompanyId = @CompanyId
    AND CAST(qsl.ScanDate AS DATE) BETWEEN @FromDate AND @ToDate;
", new
                {
                    CompanyId = company.CompanyId,
                    FromDate = fromDate,
                    ToDate = toDate
                });

                company.PendingQty = Math.Max(0, company.TotalQty - company.RedeemQty);
            }

            summary.IsOverrideApplied = await con.ExecuteScalarAsync<bool>($@"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM RequestOverride ro
    INNER JOIN RequestHeader rh
        ON rh.Id = ro.RequestHeaderId
    WHERE
        ro.IsActive = 1
        AND rh.IsActive = 1
        AND ro.FromDate <= @ToDate
        AND ro.ToDate >= @FromDate
        {companyFilter}
)
THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
", parameters);

            summary.TotalQRCodes = Convert.ToInt32(summary.MonthOrderedQty);

            return summary;
        }
        private class DashboardEffectiveRow
        {
            public int RequestDetailId { get; set; }
            public int? SessionId { get; set; }
            public int CuisineId { get; set; }
            public int? LocationId { get; set; }
            public int CompanyId { get; set; }
            public string PlanType { get; set; } = "";
            public decimal Qty { get; set; }
            public DateTime OrderDate { get; set; }
        }

      
        public class PlanTypeOrderDTO
        {
            public string PlanType { get; set; } = "";
            public decimal TotalQty { get; set; }
        }
        private string NormalizePlanType(string? value)
        {
            var text = (value ?? "").Trim().ToLower();

            if (text == "basic") return "Basic";
            if (text == "standard") return "Standard";
            if (text == "premium") return "Premium";

            return "Basic";
        }
        private class DashboardRawDetailRow
        {
            public int RequestDetailId { get; set; }
            public int RequestHeaderId { get; set; }
            public int CuisineId { get; set; }
            public string PlanType { get; set; } = "";
            public decimal Qty { get; set; }
        }

        private class DashboardRawOverrideRow
        {
            public int RequestOverrideId { get; set; }
            public int RequestDetailId { get; set; }
            public int CuisineId { get; set; }
            public string PlanType { get; set; } = "";
            public decimal Qty { get; set; }
        }

   
    }
}