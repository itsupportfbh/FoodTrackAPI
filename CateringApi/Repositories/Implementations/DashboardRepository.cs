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

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", fromDate);
            parameters.Add("@ToDate", toDate);
            parameters.Add("@Today", today);

            var sql = @"
IF OBJECT_ID('tempdb..#Final') IS NOT NULL DROP TABLE #Final;

CREATE TABLE #Final
(
    RequestHeaderId INT,
    CompanyId INT,
    CompanyName NVARCHAR(200),
    PlanType NVARCHAR(100),
    Qty DECIMAL(18,2)
);

-- ACTIVE HEADERS
;WITH AH AS
(
    SELECT rh.Id, rh.CompanyId, cm.CompanyName
    FROM RequestHeader rh
    LEFT JOIN CompanyMaster cm ON cm.Id = rh.CompanyId
    WHERE rh.IsActive = 1
),

-- LATEST OVERRIDE ONLY
LO AS
(
    SELECT *
    FROM
    (
        SELECT ro.*,
               ROW_NUMBER() OVER (PARTITION BY ro.RequestHeaderId ORDER BY ro.Id DESC) rn
        FROM RequestOverride ro
        WHERE ro.IsActive = 1
    ) x
    WHERE rn = 1
),

-- BASE (NO OVERRIDE)
BASE AS
(
    SELECT
        ah.Id,
        ah.CompanyId,
        ah.CompanyName,
        rd.PlanType,
        rd.Qty
    FROM AH ah
    INNER JOIN RequestDetail rd ON rd.RequestHeaderId = ah.Id
    WHERE rd.IsActive = 1
    AND NOT EXISTS (SELECT 1 FROM LO WHERE RequestHeaderId = ah.Id)
),

-- OVERRIDE (FULL RANGE)
OVR AS
(
    SELECT
        ah.Id,
        ah.CompanyId,
        ah.CompanyName,
        rod.PlanType,
        rod.OverrideQty AS Qty
    FROM LO ro
    INNER JOIN RequestOverrideDetail rod ON rod.RequestOverrideId = ro.Id
    INNER JOIN AH ah ON ah.Id = ro.RequestHeaderId
    WHERE rod.IsActive = 1 AND ISNULL(rod.IsCancelled,0)=0
)

-- FINAL INSERT
INSERT INTO #Final
SELECT
    Id,
    CompanyId,
    CompanyName,
    PlanType,
    Qty * (DATEDIFF(DAY, @FromDate, @ToDate) + 1)
FROM
(
    SELECT * FROM BASE
    UNION ALL
    SELECT * FROM OVR
) X;

--------------------------------------------------
-- SUMMARY
--------------------------------------------------
SELECT
    COUNT(DISTINCT CompanyId) AS TotalCompanies,
    COUNT(DISTINCT RequestHeaderId) AS TotalOrders,
    SUM(Qty) AS MonthOrderedQty,
    SUM(Qty) AS MonthPendingQty,
    0 AS MonthRedeemedQty,
    0 AS TotalPrice
FROM #Final;

--------------------------------------------------
-- PLAN TYPE
--------------------------------------------------
SELECT PlanType, SUM(Qty) AS TotalQty
FROM #Final
GROUP BY PlanType;

--------------------------------------------------
-- SESSION (PlanType as session)
--------------------------------------------------
SELECT 0 AS SessionId, PlanType AS SessionName, SUM(Qty) AS TotalQty
FROM #Final
GROUP BY PlanType;

--------------------------------------------------
-- PRICE BREAKDOWN
--------------------------------------------------
SELECT
    f.PlanType,
    0 AS SessionId,
    f.PlanType AS SessionName,
    SUM(f.Qty) AS Qty,
    ISNULL(MAX(sp.Rate),0) AS Rate,
    SUM(f.Qty * ISNULL(sp.Rate,0)) AS TotalPrice
FROM #Final f
LEFT JOIN SessionPrice sp
    ON LTRIM(RTRIM(sp.PlanType)) = LTRIM(RTRIM(f.PlanType))
    AND sp.IsActive = 1
    AND sp.SessionId = 1
GROUP BY f.PlanType;

--------------------------------------------------
-- CURRENT PRICES
--------------------------------------------------
SELECT
    sp.PlanType,
    0 AS SessionId,
    sp.PlanType AS SessionName,
    sp.Rate
FROM SessionPrice sp
WHERE sp.IsActive = 1 AND sp.SessionId = 1;

--------------------------------------------------
-- COMPANY
--------------------------------------------------
SELECT
    CompanyId,
    CompanyName,
    SUM(Qty) AS TotalQty,
    0 AS RedeemQty,
    SUM(Qty) AS PendingQty
FROM #Final
GROUP BY CompanyId, CompanyName;

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

            // ✔ FIX TOTAL PRICE
            summary.TotalPrice = summary.SessionPriceBreakdown.Sum(x => x.TotalPrice);

            // ✔ SCANS
            summary.TodayScans = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(*) FROM QrScanLog
WHERE CAST(ScanDate AS DATE) = @Today AND ISNULL(IsAllowed,0)=1", parameters);

            summary.TodayRedeemedQty = summary.TodayScans;
            summary.TodayPendingQty = summary.TodayOrderedQty - summary.TodayRedeemedQty;

            // ✔ OVERRIDE FLAG
            summary.IsOverrideApplied = await con.ExecuteScalarAsync<bool>(@"
SELECT CASE WHEN EXISTS(SELECT 1 FROM RequestOverride WHERE IsActive=1)
THEN 1 ELSE 0 END");

            summary.TotalQRCodes = (int)summary.MonthOrderedQty;

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