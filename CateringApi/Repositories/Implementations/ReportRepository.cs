using System.ComponentModel;
using System.Data;
using System.Text;
using CateringApi.Data;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using CateringApi.Services;
using Dapper;
using OfficeOpenXml;

namespace CateringApi.Repositories.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private readonly DapperContext _context;
        private readonly IEmailService _emailService;

        public ReportRepository(DapperContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

            // Convert list filters to CSV
            string? companyIdsCsv = model.CompanyIds != null && model.CompanyIds.Any()
                ? string.Join(",", model.CompanyIds.Distinct())
                : null;

            string? sessionIdsCsv = model.SessionIds != null && model.SessionIds.Any()
                ? string.Join(",", model.SessionIds.Distinct())
                : null;

            string? cuisineIdsCsv = model.CuisineIds != null && model.CuisineIds.Any()
                ? string.Join(",", model.CuisineIds.Distinct())
                : null;

            string? locationIdsCsv = model.LocationIds != null && model.LocationIds.Any()
                ? string.Join(",", model.LocationIds.Distinct())
                : null;

            // backward compatibility with old single filters
            if (string.IsNullOrWhiteSpace(companyIdsCsv) && model.CompanyId.HasValue)
                companyIdsCsv = model.CompanyId.Value.ToString();

            if (string.IsNullOrWhiteSpace(sessionIdsCsv) && model.SessionId.HasValue)
                sessionIdsCsv = model.SessionId.Value.ToString();

            if (string.IsNullOrWhiteSpace(cuisineIdsCsv) && model.CuisineId.HasValue)
                cuisineIdsCsv = model.CuisineId.Value.ToString();

            if (string.IsNullOrWhiteSpace(locationIdsCsv) && model.LocationId.HasValue)
                locationIdsCsv = model.LocationId.Value.ToString();

            // Role 2 users must always be restricted to their own company
            string? finalCompanyIdsCsv;
            if (roleId == 2)
            {
                finalCompanyIdsCsv = loggedInCompanyId > 0 ? loggedInCompanyId.ToString() : null;
            }
            else
            {
                finalCompanyIdsCsv = companyIdsCsv;
            }

            const string mainSql = @"
;WITH DateSeries AS
(
    SELECT
        rh.Id AS RequestHeaderId,
        CAST(rh.FromDate AS date) AS ReportDate,
        CAST(rh.ToDate AS date) AS EndDate
    FROM dbo.RequestHeader rh
    WHERE rh.IsActive = 1

    UNION ALL

    SELECT
        ds.RequestHeaderId,
        DATEADD(DAY, 1, ds.ReportDate),
        ds.EndDate
    FROM DateSeries ds
    WHERE ds.ReportDate < ds.EndDate
),
EffectiveRows AS
(
    SELECT
        rh.CompanyId,
        cm.CompanyName,
        ds.ReportDate,
        s.Id AS SessionId,
        s.Id AS SessionSortId,
        s.SessionName,
        cu.Id AS CuisineSortId,
        cu.CuisineName,
        l.Id AS LocationSortId,
        l.LocationName,

        CAST(
            CASE
                WHEN rod.Id IS NOT NULL
                     AND ISNULL(rod.IsCancelled, 0) = 0
                     AND ro.Id IS NOT NULL
                THEN rod.OverrideQty
                ELSE rd.Qty
            END
        AS decimal(18,2)) AS EffectiveQty,

        CAST(ISNULL(ratePick.Rate, 0) AS decimal(18,2)) AS Rate
    FROM DateSeries ds
    INNER JOIN dbo.RequestHeader rh
        ON rh.Id = ds.RequestHeaderId
       AND rh.IsActive = 1
    INNER JOIN dbo.RequestDetail rd
        ON rd.RequestHeaderId = rh.Id
       AND rd.IsActive = 1
    INNER JOIN dbo.CompanyMaster cm
        ON cm.Id = rh.CompanyId
    INNER JOIN dbo.Session s
        ON s.Id = rd.SessionId
    INNER JOIN dbo.CuisineMaster cu
        ON cu.Id = rd.CuisineId
    INNER JOIN dbo.Location l
        ON l.Id = rd.LocationId

    OUTER APPLY
    (
        SELECT TOP 1 rox.Id
        FROM dbo.RequestOverride rox
        WHERE rox.RequestHeaderId = rh.Id
          AND rox.IsActive = 1
          AND ds.ReportDate BETWEEN CAST(rox.FromDate AS date) AND CAST(rox.ToDate AS date)
        ORDER BY rox.Id DESC
    ) roPick

    LEFT JOIN dbo.RequestOverride ro
        ON ro.Id = roPick.Id

    LEFT JOIN dbo.RequestOverrideDetail rod
        ON rod.RequestOverrideId = ro.Id
       AND rod.RequestDetailId = rd.Id
       AND rod.IsActive = 1

    OUTER APPLY
    (
        SELECT TOP 1 x.Rate
        FROM
        (
            -- Current active session price
            SELECT
                sp.Rate,
                CAST(sp.EffectiveFrom AS date) AS EffectiveFrom,
                CAST('9999-12-31' AS date) AS EffectiveTo,
                1 AS SortOrder
            FROM dbo.SessionPrice sp
            WHERE sp.CompanyId = rh.CompanyId
              AND sp.SessionId = rd.SessionId
              AND sp.IsActive = 1
              AND CAST(sp.EffectiveFrom AS date) <= ds.ReportDate

            UNION ALL

            -- Historical session price
            SELECT
                sph.Rate,
                CAST(sph.EffectiveFrom AS date) AS EffectiveFrom,
                CAST(ISNULL(sph.EffectiveTo, '9999-12-31') AS date) AS EffectiveTo,
                2 AS SortOrder
            FROM dbo.SessionPriceHistory sph
            WHERE sph.CompanyId = rh.CompanyId
              AND sph.SessionId = rd.SessionId
              AND ds.ReportDate BETWEEN CAST(sph.EffectiveFrom AS date)
                                   AND CAST(ISNULL(sph.EffectiveTo, '9999-12-31') AS date)
        ) x
        WHERE ds.ReportDate BETWEEN x.EffectiveFrom AND x.EffectiveTo
        ORDER BY x.EffectiveFrom DESC, x.SortOrder ASC
    ) ratePick

    WHERE (@CompanyIdsCsv IS NULL OR rh.CompanyId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@CompanyIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@FromDate IS NULL OR ds.ReportDate >= CAST(@FromDate AS date))
      AND (@ToDate IS NULL OR ds.ReportDate <= CAST(@ToDate AS date))
      AND (@SessionIdsCsv IS NULL OR rd.SessionId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@SessionIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@CuisineIdsCsv IS NULL OR rd.CuisineId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@CuisineIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@LocationIdsCsv IS NULL OR rd.LocationId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@LocationIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
)
SELECT
    CompanyName,
    ReportDate,
    SessionName,
    CuisineName,
    LocationName,
    SUM(EffectiveQty) AS Count,
    Rate,
    SUM(EffectiveQty * Rate) AS TotalAmount
FROM EffectiveRows
GROUP BY
    CompanyName,
    ReportDate,
    SessionSortId,
    SessionName,
    CuisineSortId,
    CuisineName,
    LocationSortId,
    LocationName,
    Rate
ORDER BY
    CompanyName,
    ReportDate DESC,
    SessionSortId,
    CuisineSortId,
    LocationSortId
OPTION (MAXRECURSION 366);";

            const string totalSql = @"
;WITH DateSeries AS
(
    SELECT
        rh.Id AS RequestHeaderId,
        CAST(rh.FromDate AS date) AS ReportDate,
        CAST(rh.ToDate AS date) AS EndDate
    FROM dbo.RequestHeader rh
    WHERE rh.IsActive = 1

    UNION ALL

    SELECT
        ds.RequestHeaderId,
        DATEADD(DAY, 1, ds.ReportDate),
        ds.EndDate
    FROM DateSeries ds
    WHERE ds.ReportDate < ds.EndDate
),
EffectiveRows AS
(
    SELECT
        cu.CuisineName,
        CASE
            WHEN rod.Id IS NOT NULL
                 AND ISNULL(rod.IsCancelled, 0) = 0
                 AND ro.Id IS NOT NULL
            THEN rod.OverrideQty
            ELSE rd.Qty
        END AS EffectiveQty
    FROM DateSeries ds
    INNER JOIN dbo.RequestHeader rh
        ON rh.Id = ds.RequestHeaderId
       AND rh.IsActive = 1
    INNER JOIN dbo.RequestDetail rd
        ON rd.RequestHeaderId = rh.Id
       AND rd.IsActive = 1
    INNER JOIN dbo.CuisineMaster cu
        ON cu.Id = rd.CuisineId

    OUTER APPLY
    (
        SELECT TOP 1 rox.Id
        FROM dbo.RequestOverride rox
        WHERE rox.RequestHeaderId = rh.Id
          AND rox.IsActive = 1
          AND ds.ReportDate BETWEEN CAST(rox.FromDate AS date) AND CAST(rox.ToDate AS date)
        ORDER BY rox.Id DESC
    ) roPick

    LEFT JOIN dbo.RequestOverride ro
        ON ro.Id = roPick.Id

    LEFT JOIN dbo.RequestOverrideDetail rod
        ON rod.RequestOverrideId = ro.Id
       AND rod.RequestDetailId = rd.Id
       AND rod.IsActive = 1

    WHERE (@CompanyIdsCsv IS NULL OR rh.CompanyId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@CompanyIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@FromDate IS NULL OR ds.ReportDate >= CAST(@FromDate AS date))
      AND (@ToDate IS NULL OR ds.ReportDate <= CAST(@ToDate AS date))
      AND (@SessionIdsCsv IS NULL OR rd.SessionId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@SessionIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@CuisineIdsCsv IS NULL OR rd.CuisineId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@CuisineIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
      AND (@LocationIdsCsv IS NULL OR rd.LocationId IN
        (
            SELECT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@LocationIdsCsv, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL
        ))
)
SELECT
    CuisineName,
    SUM(EffectiveQty) AS TotalQty
FROM EffectiveRows
GROUP BY CuisineName
ORDER BY CuisineName
OPTION (MAXRECURSION 366);";

            var sqlParams = new
            {
                CompanyIdsCsv = finalCompanyIdsCsv,
                model.FromDate,
                model.ToDate,
                SessionIdsCsv = sessionIdsCsv,
                CuisineIdsCsv = cuisineIdsCsv,
                LocationIdsCsv = locationIdsCsv
            };

            var rows = await con.QueryAsync<ReportByDateRowDto>(mainSql, sqlParams);

            var totals = await con.QueryAsync<FoodTotalDto>(totalSql, sqlParams);

            return (rows, totals);
        }

        public async Task<byte[]> ExportReportExcelAsync(ReportFilterDto model)
        {
            ExcelPackage.License.SetNonCommercialOrganization("CateringApi");

            var result = await GetReportByDatesAsync(model);
            var rows = result.Rows.ToList();

            using var con = _context.CreateConnection();

            string companyText = "All companies";
            string sessionText = "All sessions";
            string cuisineText = "All cuisines";
            string locationText = "All locations";

            if (model.CompanyIds != null && model.CompanyIds.Any())
            {
                var names = await con.QueryAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id IN @Ids",
                    new { Ids = model.CompanyIds.Distinct().ToList() }
                );
                companyText = string.Join(", ", names);
            }
            else if (model.CompanyId.HasValue && model.CompanyId.Value > 0)
            {
                companyText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id = @Id",
                    new { Id = model.CompanyId.Value }
                ) ?? "All companies";
            }

            if (model.SessionIds != null && model.SessionIds.Any())
            {
                var names = await con.QueryAsync<string>(
                    "SELECT SessionName FROM dbo.Session WHERE Id IN @Ids",
                    new { Ids = model.SessionIds.Distinct().ToList() }
                );
                sessionText = string.Join(", ", names);
            }
            else if (model.SessionId.HasValue && model.SessionId.Value > 0)
            {
                sessionText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT SessionName FROM dbo.Session WHERE Id = @Id",
                    new { Id = model.SessionId.Value }
                ) ?? "All sessions";
            }

            if (model.CuisineIds != null && model.CuisineIds.Any())
            {
                var names = await con.QueryAsync<string>(
                    "SELECT CuisineName FROM dbo.CuisineMaster WHERE Id IN @Ids",
                    new { Ids = model.CuisineIds.Distinct().ToList() }
                );
                cuisineText = string.Join(", ", names);
            }
            else if (model.CuisineId.HasValue && model.CuisineId.Value > 0)
            {
                cuisineText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CuisineName FROM dbo.CuisineMaster WHERE Id = @Id",
                    new { Id = model.CuisineId.Value }
                ) ?? "All cuisines";
            }

            if (model.LocationIds != null && model.LocationIds.Any())
            {
                var names = await con.QueryAsync<string>(
                    "SELECT LocationName FROM dbo.Location WHERE Id IN @Ids",
                    new { Ids = model.LocationIds.Distinct().ToList() }
                );
                locationText = string.Join(", ", names);
            }
            else if (model.LocationId.HasValue && model.LocationId.Value > 0)
            {
                locationText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT LocationName FROM dbo.Location WHERE Id = @Id",
                    new { Id = model.LocationId.Value }
                ) ?? "All locations";
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Report By Dates");

            int row = 1;

            ws.Cells[row, 1].Value = "Report By Dates";
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Style.Font.Size = 18;
            ws.Cells[row, 1].Style.Font.Bold = true;
            row++;

            ws.Cells[row, 1].Value = "Company-wise food request report";
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Style.Font.Size = 11;
            ws.Cells[row, 1].Style.Font.Bold = true;
            row += 2;

            ws.Cells[row, 1].Value = "Company:";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 2].Value = companyText;

            ws.Cells[row, 3].Value = "From Date:";
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 4].Value = model.FromDate?.ToString("dd-MM-yyyy") ?? "";

            ws.Cells[row, 5].Value = "To Date:";
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 6].Value = model.ToDate?.ToString("dd-MM-yyyy") ?? "";
            row++;

            ws.Cells[row, 1].Value = "Session:";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 2].Value = sessionText;

            ws.Cells[row, 3].Value = "Cuisine:";
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 4].Value = cuisineText;

            ws.Cells[row, 5].Value = "Location:";
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 6].Value = locationText;
            row += 2;

            ws.Cells[row, 1].Value = "Session & Cuisine Totals";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 13;
            row++;

            var grouped = rows
                .GroupBy(x => x.SessionName)
                .Select(g => new
                {
                    Session = g.Key,
                    Total = g.Sum(x => Convert.ToDecimal(x.Count)),
                    Cuisines = g.GroupBy(x => x.CuisineName)
                                .Select(c => new
                                {
                                    Cuisine = c.Key,
                                    Total = c.Sum(x => Convert.ToDecimal(x.Count))
                                }).ToList()
                })
                .ToList();

            foreach (var session in grouped)
            {
                ws.Cells[row, 1].Value = session.Session;
                ws.Cells[row, 1].Style.Font.Bold = true;

                ws.Cells[row, 2].Value = "Total Count";
                ws.Cells[row, 2].Style.Font.Bold = true;

                ws.Cells[row, 3].Value = session.Total;
                ws.Cells[row, 3].Style.Font.Bold = true;
                row++;

                foreach (var c in session.Cuisines)
                {
                    ws.Cells[row, 2].Value = c.Cuisine;
                    ws.Cells[row, 3].Value = c.Total;
                    row++;
                }

                row++;
            }

            row++;

            ws.Cells[row, 1].Value = "Company";
            ws.Cells[row, 2].Value = "Date";
            ws.Cells[row, 3].Value = "Session";
            ws.Cells[row, 4].Value = "Cuisine";
            ws.Cells[row, 5].Value = "Location";
            ws.Cells[row, 6].Value = "Count";
            ws.Cells[row, 7].Value = "Rate (S$)";
            ws.Cells[row, 8].Value = "Total (S$)";

            using (var range = ws.Cells[row, 1, row, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
            }

            row++;

            decimal grandTotalAmount = 0;

            foreach (var item in rows)
            {
                ws.Cells[row, 1].Value = item.CompanyName;
                ws.Cells[row, 2].Value = item.ReportDate;
                ws.Cells[row, 2].Style.Numberformat.Format = "dd-MM-yyyy";
                ws.Cells[row, 3].Value = item.SessionName;
                ws.Cells[row, 4].Value = item.CuisineName;
                ws.Cells[row, 5].Value = item.LocationName;
                ws.Cells[row, 6].Value = item.Count;
                ws.Cells[row, 7].Value = item.Rate;
                ws.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";

                ws.Cells[row, 8].Value = item.TotalAmount;
                ws.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";

                grandTotalAmount += Convert.ToDecimal(item.TotalAmount);
                row++;
            }
            ws.Cells[row, 1, row, 7].Merge = true;
            ws.Cells[row, 1].Value = "Grand Total (S$)";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

            ws.Cells[row, 8].Value = grandTotalAmount;
            ws.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 8].Style.Font.Bold = true;


            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            return await package.GetAsByteArrayAsync();
        }

        public async Task SendReportEmailAsync(ReportEmailRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.ToEmail))
                throw new Exception("Recipient email is required.");

            var excelBytes = await ExportReportExcelAsync(model);

            using var con = _context.CreateConnection();

            string companyText = "AllCompanies";

            if (model.CompanyIds != null && model.CompanyIds.Any())
            {
                var names = await con.QueryAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id IN @Ids",
                    new { Ids = model.CompanyIds.Distinct().ToList() }
                );

                companyText = string.Join("_", names);
            }
            else if (model.CompanyId.HasValue && model.CompanyId.Value > 0)
            {
                companyText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id = @Id",
                    new { Id = model.CompanyId.Value }
                ) ?? "AllCompanies";
            }

            companyText = new string(companyText
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());

            if (string.IsNullOrWhiteSpace(companyText))
            {
                companyText = "AllCompanies";
            }

            var fileName = $"CSPL_ReportByDates_{DateTime.Now:dd-MM-yyyy}_{companyText}.xlsx";

            var subject = string.IsNullOrWhiteSpace(model.Subject)
                ? "Report By Dates"
                : model.Subject;

            var body = string.IsNullOrWhiteSpace(model.Body)
                ? "Please find the attached report."
                : model.Body;

            await _emailService.SendEmailWithAttachmentAsync(
                model.ToEmail,
                subject,
                body,
                excelBytes,
                fileName
            );
        }
    }
}