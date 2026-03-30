using CateringApi.Data;
using CateringApi.DTOs.Company;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly DapperContext _context;

        public CompanyRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CompanyDto>> GetAllAsync()
        {
            const string sql = @"
SELECT
    Id, CompanyCode, CompanyName, ContactPerson, ContactNo, Email,
    AddressLine1, AddressLine2, City, StateName, PostalCode,
    IsActive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
FROM dbo.CompanyMaster
ORDER BY CompanyName;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<CompanyDto>(sql);
        }

        public async Task<CompanyDto?> GetByIdAsync(int id)
        {
            const string sql = @"
SELECT
    Id, CompanyCode, CompanyName, ContactPerson, ContactNo, Email,
    AddressLine1, AddressLine2, City, StateName, PostalCode,
    IsActive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
FROM dbo.CompanyMaster
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<CompanyDto>(sql, new { Id = id });
        }

        public async Task<int> SaveAsync(CompanySaveDto dto)
        {
            using var con = _context.CreateConnection();

            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                const string updateSql = @"
UPDATE dbo.CompanyMaster
SET
    CompanyCode = @CompanyCode,
    CompanyName = @CompanyName,
    ContactPerson = @ContactPerson,
    ContactNo = @ContactNo,
    Email = @Email,
    AddressLine1 = @AddressLine1,
    AddressLine2 = @AddressLine2,
    City = @City,
    StateName = @StateName,
    PostalCode = @PostalCode,
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
INSERT INTO dbo.CompanyMaster
(
    CompanyCode, CompanyName, ContactPerson, ContactNo, Email,
    AddressLine1, AddressLine2, City, StateName, PostalCode,
    IsActive, CreatedBy, CreatedDate
)
VALUES
(
    @CompanyCode, @CompanyName, @ContactPerson, @ContactNo, @Email,
    @AddressLine1, @AddressLine2, @City, @StateName, @PostalCode,
    @IsActive, @UserId, GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await con.ExecuteScalarAsync<int>(insertSql, dto);
            }
        }

        public async Task<bool> DeleteAsync(int id, int? userId)
        {
            const string sql = @"
UPDATE dbo.CompanyMaster
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