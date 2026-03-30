using CateringApi.Data;
using CateringApi.DTOs.Employee;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly DapperContext _context;

        public EmployeeRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
        {
            const string sql = @"
SELECT
    e.Id,
    e.EmployeeCode,
    e.EmployeeName,
    e.CompanyId,
    c.CompanyName,
    e.DepartmentName,
    e.MobileNo,
    e.Email,
    e.QRCodeValue,
    e.IsActive
FROM dbo.EmployeeMaster e
INNER JOIN dbo.CompanyMaster c ON c.Id = e.CompanyId
ORDER BY e.EmployeeName;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<EmployeeDto>(sql);
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            const string sql = @"
SELECT
    e.Id,
    e.EmployeeCode,
    e.EmployeeName,
    e.CompanyId,
    c.CompanyName,
    e.DepartmentName,
    e.MobileNo,
    e.Email,
    e.QRCodeValue,
    e.IsActive
FROM dbo.EmployeeMaster e
INNER JOIN dbo.CompanyMaster c ON c.Id = e.CompanyId
WHERE e.Id = @Id;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<EmployeeDto>(sql, new { Id = id });
        }

        public async Task<int> SaveAsync(EmployeeSaveDto dto)
        {
            using var con = _context.CreateConnection();

            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                const string updateSql = @"
UPDATE dbo.EmployeeMaster
SET
    EmployeeCode = @EmployeeCode,
    EmployeeName = @EmployeeName,
    CompanyId = @CompanyId,
    DepartmentName = @DepartmentName,
    MobileNo = @MobileNo,
    Email = @Email,
    QRCodeValue = @QRCodeValue,
    IsActive = @IsActive,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;

SELECT @Id;";

                return await con.ExecuteScalarAsync<int>(updateSql, dto);
            }
            else
            {
                const string insertSql = @"
INSERT INTO dbo.EmployeeMaster
(
    EmployeeCode, EmployeeName, CompanyId, DepartmentName,
    MobileNo, Email, QRCodeValue, IsActive,
    CreatedBy, CreatedDate
)
VALUES
(
    @EmployeeCode, @EmployeeName, @CompanyId, @DepartmentName,
    @MobileNo, @Email, @QRCodeValue, @IsActive,
    @UserId, GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await con.ExecuteScalarAsync<int>(insertSql, dto);
            }
        }

        public async Task<bool> DeleteAsync(int id, int? userId)
        {
            const string sql = @"
UPDATE dbo.EmployeeMaster
SET IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }
    }
}