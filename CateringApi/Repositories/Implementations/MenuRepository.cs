using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.Menu;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;
using System.Globalization;
using System.Reflection.Metadata;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CateringApi.Repositories.Implementations
{
    public class MenuRepository : IMenuRepository
    {
        private readonly DapperContext _context;

        public MenuRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<SaveMenuUploadResultDto> SaveMenuUploadAsync(SaveMenuUploadRequestDto request)
        {
            if (request == null)
            {
                return new SaveMenuUploadResultDto
                {
                    Success = false,
                    Message = "Request is null.",
                    InsertedCount = 0
                };
            }

            if (request.Rows == null || !request.Rows.Any())
            {
                return new SaveMenuUploadResultDto
                {
                    Success = false,
                    Message = "No rows found to save.",
                    InsertedCount = 0
                };
            }

            using var con = _context.CreateConnection();
            if (con.State != ConnectionState.Open)
                con.Open();

            using var tran = con.BeginTransaction();

            try
            {
                const string deleteSql = @"
DELETE FROM dbo.MenuUpload
WHERE MenuMonth = @MenuMonth
  AND MenuYear = @MenuYear;";

                await con.ExecuteAsync(deleteSql, new
                {
                    MenuMonth = request.MenuMonth,
                    MenuYear = request.MenuYear
                }, tran);

                const string insertSql = @"
INSERT INTO dbo.MenuUpload
(
    MenuMonth,
    MenuYear,
    MenuDate,
    SessionName,
    CuisineName,
    SetName,
    Item1,
    Item2,
    Item3,
    Item4,
    Notes,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @MenuMonth,
    @MenuYear,
    @MenuDate,
    @SessionName,
    @CuisineName,
    @SetName,
    @Item1,
    @Item2,
    @Item3,
    @Item4,
    @Notes,
    1,
    @CreatedBy,
    GETDATE()
);";

                int insertedCount = 0;

                foreach (var row in request.Rows)
                {
                    DateTime parsedDate = ParseDate(row.Date);

                    await con.ExecuteAsync(insertSql, new
                    {
                        MenuMonth = request.MenuMonth,
                        MenuYear = request.MenuYear,
                        MenuDate = parsedDate.Date,
                        SessionName = (row.SessionName ?? string.Empty).Trim(),
                        CuisineName = (row.CuisineName ?? string.Empty).Trim(),
                        SetName = (row.SetName ?? string.Empty).Trim(),
                        Item1 = string.IsNullOrWhiteSpace(row.Item1) ? null : row.Item1.Trim(),
                        Item2 = string.IsNullOrWhiteSpace(row.Item2) ? null : row.Item2.Trim(),
                        Item3 = string.IsNullOrWhiteSpace(row.Item3) ? null : row.Item3.Trim(),
                        Item4 = string.IsNullOrWhiteSpace(row.Item4) ? null : row.Item4.Trim(),
                        Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
                        CreatedBy = request.CreatedBy
                    }, tran);

                    insertedCount++;
                }

                tran.Commit();

                return new SaveMenuUploadResultDto
                {
                    Success = true,
                    Message = "Menu uploaded successfully.",
                    InsertedCount = insertedCount
                };
            }
            catch (Exception ex)
            {
                tran.Rollback();

                return new SaveMenuUploadResultDto
                {
                    Success = false,
                    Message = ex.Message,
                    InsertedCount = 0
                };
            }
        }

        public async Task<List<MenuUploadResponseDto>> GetMenuByMonthYearAsync(int month, int year)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    Id,
    CONVERT(VARCHAR(10), MenuDate, 23) AS [Date],
    SessionName,
    CuisineName,
    SetName,
    Item1,
    Item2,
    Item3,
    Item4,
    Notes
FROM dbo.MenuUpload
WHERE MenuMonth = @Month
  AND MenuYear = @Year
  AND IsActive = 1
ORDER BY MenuDate, SessionName, CuisineName, SetName;";

            var result = await con.QueryAsync<MenuUploadResponseDto>(sql, new
            {
                Month = month,
                Year = year
            });

            return result.ToList();
        }

        private DateTime ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Date is required.");

            value = value.Trim();

            string[] formats =
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "dd-MM-yyyy",
                "d-M-yyyy",
                "MM/dd/yyyy",
                "M/d/yyyy"
            };

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;

            if (DateTime.TryParse(value, out parsed))
                return parsed;

            throw new Exception($"Invalid date format: {value}");
        }
        public async Task<List<MenuUploadResponseDto>> GetMenuByDateAsync(DateTime menuDate)
        {
            using var con = _context.CreateConnection();

            const string sql = @"
SELECT
    Id,
    CONVERT(VARCHAR(10), MenuDate, 23) AS [Date],
    SessionName,
    CuisineName,
    SetName,
    Item1,
    Item2,
    Item3,
    Item4,
    Notes
FROM dbo.MenuUpload
WHERE CAST(MenuDate AS DATE) = CAST(@MenuDate AS DATE)
  AND IsActive = 1
ORDER BY SessionName, CuisineName, SetName;";

            var result = await con.QueryAsync<MenuUploadResponseDto>(sql, new
            {
                MenuDate = menuDate.Date
            });

            return result.ToList();
        }
        public async Task<byte[]> GenerateMenuPdfAsync(DateTime menuDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var menuRows = await this.GetMenuByDateAsync(menuDate);

            if (menuRows == null || !menuRows.Any())
                throw new Exception("No menu available for selected date.");

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("FoodTrack Daily Menu")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#6F3C2F");

                        col.Item().Text($"Menu Date: {menuDate:dd-MM-yyyy}")
                            .FontSize(11);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f); // Session
                            columns.RelativeColumn(1.2f); // Cuisine
                            columns.RelativeColumn(1);   // Set
                            columns.RelativeColumn(2);   // Items
                            columns.RelativeColumn(1.2f); // Notes
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Session").Bold();
                            header.Cell().Element(CellStyle).Text("Cuisine").Bold();
                            header.Cell().Element(CellStyle).Text("Set").Bold();
                            header.Cell().Element(CellStyle).Text("Items").Bold();
                            header.Cell().Element(CellStyle).Text("Notes").Bold();
                        });

                        foreach (var row in menuRows)
                        {
                            var items = string.Join(", ",
                                new[] { row.Item1, row.Item2, row.Item3, row.Item4 }
                                .Where(x => !string.IsNullOrWhiteSpace(x)));

                            table.Cell().Element(CellStyle).Text(row.SessionName ?? "-");
                            table.Cell().Element(CellStyle).Text(row.CuisineName ?? "-");
                            table.Cell().Element(CellStyle).Text(row.SetName ?? "-");
                            table.Cell().Element(CellStyle).Text(items);
                            table.Cell().Element(CellStyle).Text(row.Notes ?? "-");
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(6);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by FoodTrack");
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }
        public async Task<byte[]> GenerateMonthlyMenuPdfAsync(int month, int year)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var rows = await this.GetMenuByMonthYearAsync(month, year);

            if (rows == null || !rows.Any())
                throw new Exception("No menu found for selected month.");

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Catering Solutions Monthly Menu")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#6F3C2F");

                        col.Item().Text($"Month: {month:D2}/{year}")
                            .FontSize(11);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.1f); // Date
                            columns.RelativeColumn(1.1f); // Session
                            columns.RelativeColumn(1.1f); // Cuisine
                            columns.RelativeColumn(0.9f); // Set
                            columns.RelativeColumn(2.3f); // Items
                            columns.RelativeColumn(1.2f); // Notes
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Date").Bold();
                            header.Cell().Element(CellStyle).Text("Session").Bold();
                            header.Cell().Element(CellStyle).Text("Cuisine").Bold();
                            header.Cell().Element(CellStyle).Text("Set").Bold();
                            header.Cell().Element(CellStyle).Text("Items").Bold();
                            header.Cell().Element(CellStyle).Text("Notes").Bold();
                        });

                        foreach (var row in rows)
                        {
                            var items = string.Join(", ",
                                new[] { row.Item1, row.Item2, row.Item3, row.Item4 }
                                .Where(x => !string.IsNullOrWhiteSpace(x)));

                            table.Cell().Element(CellStyle).Text(row.Date ?? "-");
                            table.Cell().Element(CellStyle).Text(row.SessionName ?? "-");
                            table.Cell().Element(CellStyle).Text(row.CuisineName ?? "-");
                            table.Cell().Element(CellStyle).Text(row.SetName ?? "-");
                            table.Cell().Element(CellStyle).Text(items);
                            table.Cell().Element(CellStyle).Text(row.Notes ?? "-");
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5);
                        }
                    });

                    page.Footer().AlignCenter().Text("Generated by FoodTrack");
                });
            }).GeneratePdf();

            return pdfBytes;
        }
    }
}