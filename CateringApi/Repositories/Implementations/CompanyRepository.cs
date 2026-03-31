using BCrypt.Net;
using CateringApi.Data;
using CateringApi.DTOs.Company;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;

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
    Id,
    CompanyCode,
    CompanyName,
    ContactPerson,
    ContactNo,
    Email,
    AddressLine1,
    AddressLine2,
    City,
    StateName,
    PostalCode,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate
FROM dbo.CompanyMaster where isactive = 1
ORDER BY CompanyName;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<CompanyDto>(sql);
        }

        public async Task<CompanySaveDto?> GetByIdAsync(int id)
        {
            using var con = _context.CreateConnection();

            const string companySql = @"
SELECT 
    Id,
    CompanyCode,
    CompanyName,
    ContactPerson,
    ContactNo,
    Email,
    AddressLine1,
    AddressLine2,
    City,
    StateName,
    PostalCode,
    IsActive
FROM dbo.CompanyMaster
WHERE Id = @Id;";

            const string userSql = @"
SELECT TOP 1
    Username,
    Email
    
FROM dbo.UserMaster
WHERE CompanyId = @Id
ORDER BY Id;";

            const string locationSql = @"
SELECT LocationId
FROM dbo.CompanyLocationMap
WHERE CompanyId = @Id;";

            const string sessionSql = @"
SELECT SessionId
FROM dbo.CompanySessionMap
WHERE CompanyId = @Id;";

            const string cuisineSql = @"
SELECT CuisineId
FROM dbo.CompanyCuisineMap
WHERE CompanyId = @Id;";

            var company = await con.QueryFirstOrDefaultAsync<CompanySaveDto>(companySql, new { Id = id });

            if (company == null)
                return null;

            var user = await con.QueryFirstOrDefaultAsync(userSql, new { Id = id });

            //if (user != null)
            //{
            //    company.ContactPerson = user.Username;
            //    company.ContactNo = user.ContactNo;
            //}

            company.LocationIds = (await con.QueryAsync<int>(locationSql, new { Id = id })).ToList();
            company.SessionIds = (await con.QueryAsync<int>(sessionSql, new { Id = id })).ToList();
            company.CuisineIds = (await con.QueryAsync<int>(cuisineSql, new { Id = id })).ToList();

            company.Password = string.Empty;

            return company;
        }

        public async Task<int> SaveAsync(CompanySaveDto dto)
        {
            using var con = _context.CreateConnection();
            if (con.State != ConnectionState.Open)
                con.Open();

            using var tx = con.BeginTransaction();

            try
            {
                // Duplicate company code check
                var existingCompanyCode = await con.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.CompanyMaster
                      WHERE CompanyCode = @CompanyCode
                        AND (@Id IS NULL OR Id <> @Id);",
                    new { dto.CompanyCode, dto.Id },
                    tx);

                if (existingCompanyCode > 0)
                    throw new Exception("Company code already exists.");

                // Duplicate email check in company table
                var existingCompanyEmail = await con.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.CompanyMaster
                      WHERE Email = @Email
                        AND (@Id IS NULL OR Id <> @Id);",
                    new { dto.Email, dto.Id },
                    tx);

                if (existingCompanyEmail > 0)
                    throw new Exception("Company email already exists.");

                if (dto.Id.HasValue && dto.Id.Value > 0)
                {
                    const string updateSql = @"
UPDATE dbo.CompanyMaster
SET
    CompanyCode   = @CompanyCode,
    CompanyName   = @CompanyName,
    ContactPerson = @ContactPerson,
    ContactNo     = @ContactNo,
    Email         = @Email,
    AddressLine1  = @AddressLine1,
    AddressLine2  = @AddressLine2,
    City          = @City,
    StateName     = @StateName,
    PostalCode    = @PostalCode,
    IsActive      = @IsActive,
    UpdatedBy     = @UserId,
    UpdatedDate   = GETDATE()
WHERE Id = @Id;

SELECT @Id;";

                    var companyId = await con.ExecuteScalarAsync<int>(updateSql, dto, tx);

                    // Update linked login user also
                    const string updateUserSql = @"
UPDATE dbo.UserMaster
SET
    Username    = @Email,
    Email       = @Email,
    IsActive    = @IsActive,
    UpdatedBy   = @UserId,
    UpdatedDate = GETDATE()
WHERE CompanyId = @CompanyId
  AND RoleId = (
        SELECT TOP 1 Id
        FROM dbo.RoleMaster
        WHERE RoleCode = 'ADMIN'
    );";

                    await con.ExecuteAsync(updateUserSql, new
                    {
                        Email = dto.Email,
                        IsActive = dto.IsActive,
                        UserId = dto.UserId,
                        CompanyId = companyId
                    }, tx);

                    // Optional password update if password passed
                    if (!string.IsNullOrWhiteSpace(dto.Password))
                    {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                        const string updatePasswordSql = @"
UPDATE dbo.UserMaster
SET
    PasswordHash = @PasswordHash,
    UpdatedBy    = @UserId,
    UpdatedDate  = GETDATE()
WHERE CompanyId = @CompanyId
  AND RoleId = (
        SELECT TOP 1 Id
        FROM dbo.RoleMaster
        WHERE RoleCode = 'ADMIN'
    );";

                        await con.ExecuteAsync(updatePasswordSql, new
                        {
                            PasswordHash = passwordHash,
                            UserId = dto.UserId,
                            CompanyId = companyId
                        }, tx);
                    }
                    await SaveCompanyMappingsAsync(con, tx, dto, companyId);
                    tx.Commit();
                    return companyId;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.Password))
                        throw new Exception("Password is required for new company.");

                    // Check duplicate username/email in user table
                    var existingUsername = await con.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(1)
                          FROM dbo.UserMaster
                          WHERE Username = @Email OR Email = @Email;",
                        new { dto.Email },
                        tx);

                    if (existingUsername > 0)
                        throw new Exception("Login email already exists.");

                    var adminRoleId = await con.ExecuteScalarAsync<int?>(
                        @"SELECT TOP 1 Id
                          FROM dbo.RoleMaster
                          WHERE RoleCode = 'ADMIN' AND IsActive = 1;",
                        transaction: tx);

                    if (!adminRoleId.HasValue)
                        throw new Exception("ADMIN role not found.");

                    const string insertCompanySql = @"
INSERT INTO dbo.CompanyMaster
(
    CompanyCode,
    CompanyName,
    ContactPerson,
    ContactNo,
    Email,
    AddressLine1,
    AddressLine2,
    City,
    StateName,
    PostalCode,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @CompanyCode,
    @CompanyName,
    @ContactPerson,
    @ContactNo,
    @Email,
    @AddressLine1,
    @AddressLine2,
    @City,
    @StateName,
    @PostalCode,
    @IsActive,
    @UserId,
    GETDATE()
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var companyId = await con.ExecuteScalarAsync<int>(insertCompanySql, dto, tx);

                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                    const string insertUserSql = @"
INSERT INTO dbo.UserMaster
(
    CompanyId,
    RoleId,
    Username,
    Email,
    PasswordHash,
    IsActive,
    CreatedBy,
    CreatedDate
)
VALUES
(
    @CompanyId,
    @RoleId,
    @Username,
    @Email,
    @PasswordHash,
    1,
    @CreatedBy,
    GETDATE()
);";

                    await con.ExecuteAsync(insertUserSql, new
                    {
                        CompanyId = companyId,
                        RoleId = adminRoleId.Value,
                        Username = dto.Email,   // email தான் username
                        Email = dto.Email,
                        PasswordHash = passwordHash,
                        CreatedBy = dto.UserId
                    }, tx);
                    await SaveCompanyMappingsAsync(con, tx, dto, companyId);
                    tx.Commit();
                    return companyId;
                }
            }
            catch
            {
                tx.Rollback();
                throw;
               
            }
        }

        public async Task<bool> DeleteAsync(int id, int? userId)
        {
            using var con = _context.CreateConnection();
            if (con.State != ConnectionState.Open)
                con.Open();

            using var tx = con.BeginTransaction();

            try
            {
                const string sql1 = @"
UPDATE dbo.CompanyMaster
SET
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE Id = @Id;";

                const string sql2 = @"
UPDATE dbo.UserMaster
SET
    IsActive = 0,
    UpdatedBy = @UserId,
    UpdatedDate = GETDATE()
WHERE CompanyId = @Id;";

                var rows = await con.ExecuteAsync(sql1, new { Id = id, UserId = userId }, tx);
                await con.ExecuteAsync(sql2, new { Id = id, UserId = userId }, tx);

                tx.Commit();
                return rows > 0;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        private async Task SaveCompanyMappingsAsync(IDbConnection con, IDbTransaction tx, CompanySaveDto dto, int companyId)
        {
            await con.ExecuteAsync("DELETE FROM dbo.CompanyLocationMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            await con.ExecuteAsync("DELETE FROM dbo.CompanySessionMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            await con.ExecuteAsync("DELETE FROM dbo.CompanyCuisineMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            if (dto.LocationIds != null && dto.LocationIds.Any())
            {
                const string locationSql = @"
INSERT INTO dbo.CompanyLocationMap (CompanyId, LocationId, CreatedBy, CreatedDate)
VALUES (@CompanyId, @LocationId, @CreatedBy, GETDATE());";

                foreach (var locationId in dto.LocationIds.Distinct())
                {
                    await con.ExecuteAsync(locationSql, new
                    {
                        CompanyId = companyId,
                        LocationId = locationId,
                        CreatedBy = dto.UserId
                    }, tx);
                }
            }

            if (dto.SessionIds != null && dto.SessionIds.Any())
            {
                const string sessionSql = @"
INSERT INTO dbo.CompanySessionMap (CompanyId, SessionId, CreatedBy, CreatedDate)
VALUES (@CompanyId, @SessionId, @CreatedBy, GETDATE());";

                foreach (var sessionId in dto.SessionIds.Distinct())
                {
                    await con.ExecuteAsync(sessionSql, new
                    {
                        CompanyId = companyId,
                        SessionId = sessionId,
                        CreatedBy = dto.UserId
                    }, tx);
                }
            }

            if (dto.CuisineIds != null && dto.CuisineIds.Any())
            {
                const string cuisineSql = @"
INSERT INTO dbo.CompanyCuisineMap (CompanyId, CuisineId, CreatedBy, CreatedDate)
VALUES (@CompanyId, @CuisineId, @CreatedBy, GETDATE());";

                foreach (var cuisineId in dto.CuisineIds.Distinct())
                {
                    await con.ExecuteAsync(cuisineSql, new
                    {
                        CompanyId = companyId,
                        CuisineId = cuisineId,
                        CreatedBy = dto.UserId
                    }, tx);
                }
            }
        }
    }
}