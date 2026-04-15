using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.Menu;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;
using System.Globalization;

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
    }
}