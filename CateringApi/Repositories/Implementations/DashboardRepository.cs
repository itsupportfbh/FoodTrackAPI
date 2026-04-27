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
IF OBJECT_ID('tempdb..#Final') IS NOT NULL
    DROP TABLE #Final;

CREATE TABLE #Final
(
    RequestHeaderId INT,
    CompanyId INT,
    CompanyName NVARCHAR(250),
    PlanType NVARCHAR(100),
    Qty DECIMAL(18,2)
);

;WITH ActiveHeaders AS
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
        x.RequestHeaderId
    FROM
    (
        SELECT
            ro.Id,
            ro.RequestHeaderId,
            ROW_NUMBER() OVER
            (
                PARTITION BY ro.RequestHeaderId
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
BaseRows AS
(
    SELECT
        ah.Id AS RequestHeaderId,
        ah.CompanyId,
        ah.CompanyName,
        ISNULL(NULLIF(LTRIM(RTRIM(rd.PlanType)), ''), 'Basic') AS PlanType,
        ISNULL(rd.Qty, 0) *
        (
            DATEDIFF(
                DAY,
                CASE WHEN ah.FromDate < @FromDate THEN @FromDate ELSE ah.FromDate END,
                CASE WHEN ah.ToDate > @ToDate THEN @ToDate ELSE ah.ToDate END
            ) + 1
        ) AS Qty
    FROM ActiveHeaders ah
    INNER JOIN RequestDetail rd
        ON rd.RequestHeaderId = ah.Id
    WHERE
        rd.IsActive = 1
        AND NOT EXISTS
        (
            SELECT 1
            FROM LatestOverride lo
            WHERE lo.RequestHeaderId = ah.Id
        )
),
OverrideRows AS
(
    SELECT
        ah.Id AS RequestHeaderId,
        ah.CompanyId,
        ah.CompanyName,
        ISNULL(NULLIF(LTRIM(RTRIM(rod.PlanType)), ''), 'Basic') AS PlanType,
        ISNULL(rod.OverrideQty, 0) *
        (
            DATEDIFF(DAY, @FromDate, @ToDate) + 1
        ) AS Qty
    FROM LatestOverride lo
    INNER JOIN RequestOverrideDetail rod
        ON rod.RequestOverrideId = lo.Id
    INNER JOIN ActiveHeaders ah
        ON ah.Id = lo.RequestHeaderId
    WHERE
        rod.IsActive = 1
        AND ISNULL(rod.IsCancelled, 0) = 0
)
INSERT INTO #Final
(
    RequestHeaderId,
    CompanyId,
    CompanyName,
    PlanType,
    Qty
)
SELECT
    br.RequestHeaderId,
    br.CompanyId,
    br.CompanyName,
    br.PlanType,
    br.Qty
FROM BaseRows br
WHERE br.Qty > 0

UNION ALL

SELECT
    orow.RequestHeaderId,
    orow.CompanyId,
    orow.CompanyName,
    orow.PlanType,
    orow.Qty
FROM OverrideRows orow
WHERE orow.Qty > 0;

SELECT
    COUNT(DISTINCT f.CompanyId) AS TotalCompanies,
    COUNT(DISTINCT f.RequestHeaderId) AS TotalOrders,
    ISNULL(SUM(CASE WHEN @Today BETWEEN @FromDate AND @ToDate THEN f.Qty ELSE 0 END), 0) AS TodayOrderedQty,
    0 AS TodayRedeemedQty,
    ISNULL(SUM(CASE WHEN @Today BETWEEN @FromDate AND @ToDate THEN f.Qty ELSE 0 END), 0) AS TodayPendingQty,
    ISNULL(SUM(f.Qty), 0) AS MonthOrderedQty,
    0 AS MonthRedeemedQty,
    ISNULL(SUM(f.Qty), 0) AS MonthPendingQty,
    ISNULL(SUM(f.Qty * ISNULL(sp.Rate, 0)), 0) AS TotalPrice
FROM #Final f
LEFT JOIN SessionPrice sp
    ON LTRIM(RTRIM(sp.PlanType)) = LTRIM(RTRIM(f.PlanType))
    AND sp.IsActive = 1
    AND sp.SessionId = 1
    AND CAST(sp.EffectiveFrom AS DATE) <= @ToDate;

SELECT
    f.PlanType,
    ISNULL(SUM(f.Qty), 0) AS TotalQty
FROM #Final f
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    0 AS SessionId,
    f.PlanType AS SessionName,
    ISNULL(SUM(f.Qty), 0) AS TotalQty
FROM #Final f
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    f.PlanType,
    0 AS SessionId,
    f.PlanType AS SessionName,
    ISNULL(SUM(f.Qty), 0) AS Qty,
    ISNULL(MAX(sp.Rate), 0) AS Rate,
    ISNULL(SUM(f.Qty * ISNULL(sp.Rate, 0)), 0) AS TotalPrice
FROM #Final f
LEFT JOIN SessionPrice sp
    ON LTRIM(RTRIM(sp.PlanType)) = LTRIM(RTRIM(f.PlanType))
    AND sp.IsActive = 1
    AND sp.SessionId = 1
    AND CAST(sp.EffectiveFrom AS DATE) <= @ToDate
GROUP BY f.PlanType
ORDER BY f.PlanType;

SELECT
    sp.PlanType,
    0 AS SessionId,
    sp.PlanType AS SessionName,
    sp.Rate
FROM SessionPrice sp
WHERE
    sp.IsActive = 1
    AND sp.SessionId = 1
    AND CAST(sp.EffectiveFrom AS DATE) <= @ToDate
ORDER BY sp.PlanType;

SELECT
    f.CompanyId,
    ISNULL(f.CompanyName, 'Unknown Company') AS CompanyName,
    ISNULL(SUM(f.Qty), 0) AS TotalQty,
    0 AS RedeemQty,
    ISNULL(SUM(f.Qty), 0) AS PendingQty
FROM #Final f
GROUP BY f.CompanyId, f.CompanyName
ORDER BY TotalQty DESC;

DROP TABLE #Final;
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