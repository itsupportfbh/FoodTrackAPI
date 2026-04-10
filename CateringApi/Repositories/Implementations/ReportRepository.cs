using System.ComponentModel;
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

            int? finalCompanyId = roleId == 2
                ? loggedInCompanyId
                : model.CompanyId;

            // 🔹 MAIN REPORT
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
)
SELECT
    cm.CompanyName,
    ds.ReportDate,
    s.SessionName,
    cu.CuisineName,
    l.LocationName,
    SUM(rd.Qty) AS Count
FROM DateSeries ds
INNER JOIN dbo.RequestHeader rh
    ON rh.Id = ds.RequestHeaderId
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
WHERE rh.IsActive = 1
  AND (@CompanyId IS NULL OR rh.CompanyId = @CompanyId)
  AND (@FromDate IS NULL OR ds.ReportDate >= CAST(@FromDate AS date))
  AND (@ToDate IS NULL OR ds.ReportDate <= CAST(@ToDate AS date))
  AND (@SessionId IS NULL OR rd.SessionId = @SessionId)
  AND (@CuisineId IS NULL OR rd.CuisineId = @CuisineId)
  AND (@LocationId IS NULL OR rd.LocationId = @LocationId)
GROUP BY
    cm.CompanyName,
    ds.ReportDate,
    s.Id,
    s.SessionName,
    cu.Id,
    cu.CuisineName,
    l.Id,
    l.LocationName
ORDER BY
    cm.CompanyName,
    ds.ReportDate DESC,
    s.Id,
    cu.Id,
    l.Id
OPTION (MAXRECURSION 366);";

            // 🔥 FOOD TOTAL (NEW)
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
)
SELECT
    cu.CuisineName,
    SUM(rd.Qty) AS TotalQty
FROM DateSeries ds
INNER JOIN dbo.RequestHeader rh
    ON rh.Id = ds.RequestHeaderId
INNER JOIN dbo.RequestDetail rd
    ON rd.RequestHeaderId = rh.Id
   AND rd.IsActive = 1
INNER JOIN dbo.CuisineMaster cu
    ON cu.Id = rd.CuisineId
WHERE rh.IsActive = 1
  AND (@CompanyId IS NULL OR rh.CompanyId = @CompanyId)
  AND (@FromDate IS NULL OR ds.ReportDate >= CAST(@FromDate AS date))
  AND (@ToDate IS NULL OR ds.ReportDate <= CAST(@ToDate AS date))
  AND (@SessionId IS NULL OR rd.SessionId = @SessionId)
  AND (@CuisineId IS NULL OR rd.CuisineId = @CuisineId)
  AND (@LocationId IS NULL OR rd.LocationId = @LocationId)
GROUP BY cu.CuisineName
ORDER BY cu.CuisineName
OPTION (MAXRECURSION 366);";

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

            if (model.CompanyId.HasValue && model.CompanyId.Value > 0)
            {
                companyText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id = @Id",
                    new { Id = model.CompanyId.Value }
                ) ?? "All companies";
            }

            if (model.SessionId.HasValue && model.SessionId.Value > 0)
            {
                sessionText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT SessionName FROM dbo.Session WHERE Id = @Id",
                    new { Id = model.SessionId.Value }
                ) ?? "All sessions";
            }

            if (model.CuisineId.HasValue && model.CuisineId.Value > 0)
            {
                cuisineText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CuisineName FROM dbo.CuisineMaster WHERE Id = @Id",
                    new { Id = model.CuisineId.Value }
                ) ?? "All cuisines";
            }

            if (model.LocationId.HasValue && model.LocationId.Value > 0)
            {
                locationText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT LocationName FROM dbo.Location WHERE Id = @Id",
                    new { Id = model.LocationId.Value }
                ) ?? "All locations";
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Report By Dates");

            int row = 1;

            // Header
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

            // Filters
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

            // Session & Cuisine Totals
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

            // Table Header
            ws.Cells[row, 1].Value = "Company";
            ws.Cells[row, 2].Value = "Date";
            ws.Cells[row, 3].Value = "Session";
            ws.Cells[row, 4].Value = "Cuisine";
            ws.Cells[row, 5].Value = "Location";
            ws.Cells[row, 6].Value = "Count";

            using (var range = ws.Cells[row, 1, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
            }

            row++;

            // Table Data
            foreach (var item in rows)
            {
                ws.Cells[row, 1].Value = item.CompanyName;
                ws.Cells[row, 2].Value = item.ReportDate;
                ws.Cells[row, 2].Style.Numberformat.Format = "dd-MM-yyyy";
                ws.Cells[row, 3].Value = item.SessionName;
                ws.Cells[row, 4].Value = item.CuisineName;
                ws.Cells[row, 5].Value = item.LocationName;
                ws.Cells[row, 6].Value = item.Count;
                row++;
            }

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
            string companyText = "AllCompanies";

            if (model.CompanyId.HasValue && model.CompanyId.Value > 0)
            {
                using var con = _context.CreateConnection();

                companyText = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT CompanyName FROM dbo.CompanyMaster WHERE Id = @Id",
                    new { Id = model.CompanyId.Value }
                ) ?? "AllCompanies";
            }

            // safe file name
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