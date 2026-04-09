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

        public async Task<IEnumerable<CompanyMaster>> GetAllAsync()
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
            return await con.QueryAsync<CompanyMaster>(sql);
        }

        public async Task<CompanyDetailDto?> GetByIdAsync(int id)
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

            var company = await con.QueryFirstOrDefaultAsync<CompanyDetailDto>(companySql, new { Id = id });
            if (company == null)
                return null;

            var locationIds = await con.QueryAsync<int>(
                @"SELECT LocationId
          FROM dbo.CompanyLocationMap
          WHERE CompanyId = @Id;",
                new { Id = id });

            var cuisineIds = await con.QueryAsync<int>(
                @"SELECT CuisineId
          FROM dbo.CompanyCuisineMap
          WHERE CompanyId = @Id;",
                new { Id = id });

            var sessionTimings = await con.QueryAsync<CompanySessionTimeDto>(
                @"SELECT
              csm.SessionId,
              sm.SessionName,
              csm.FromTime,
              csm.ToTime
          FROM dbo.CompanySessionMapping csm
          INNER JOIN dbo.Session sm ON sm.Id = csm.SessionId
          WHERE csm.CompanyId = @Id
            AND csm.IsActive = 1;",
                new { Id = id });

            company.LocationIds = locationIds.ToList();
            company.CuisineIds = cuisineIds.ToList();
            company.SessionTimings = sessionTimings.ToList();

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
                var existingCompanyCode = await con.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                  FROM dbo.CompanyMaster
                  WHERE CompanyCode = @CompanyCode
                    AND (@Id IS NULL OR Id <> @Id);",
                    new { dto.CompanyCode, dto.Id },
                    tx);

                if (existingCompanyCode > 0)
                    throw new Exception("Company code already exists.");

                var existingCompanyEmail = await con.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                  FROM dbo.CompanyMaster
                  WHERE Email = @Email
                    AND (@Id IS NULL OR Id <> @Id);",
                    new { dto.Email, dto.Id },
                    tx);

                if (existingCompanyEmail > 0)
                    throw new Exception("Company email already exists.");

                int companyId;

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

                    companyId = await con.ExecuteScalarAsync<int>(updateSql, dto, tx);

                    const string updateUserSql = @"
UPDATE dbo.UserMaster
SET
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
                        dto.Email,
                        dto.IsActive,
                        dto.UserId,
                        CompanyId = companyId
                    }, tx);

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
                            dto.UserId,
                            CompanyId = companyId
                        }, tx);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.Password))
                        throw new Exception("Password is required for new company.");

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

                    companyId = await con.ExecuteScalarAsync<int>(insertCompanySql, dto, tx);

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
                        Username = dto.Email,
                        Email = dto.Email,
                        PasswordHash = passwordHash,
                        CreatedBy = dto.UserId
                    }, tx);
                }

                await SaveCompanyMappingsAsync(con, tx, dto, companyId);

                tx.Commit();
                return companyId;
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
            await con.ExecuteAsync(
                "DELETE FROM dbo.CompanyLocationMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            await con.ExecuteAsync(
                "DELETE FROM dbo.CompanySessionMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            await con.ExecuteAsync(
                "DELETE FROM dbo.CompanySessionMapping WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            await con.ExecuteAsync(
                "DELETE FROM dbo.CompanyCuisineMap WHERE CompanyId = @CompanyId;",
                new { CompanyId = companyId }, tx);

            if (dto.LocationIds != null && dto.LocationIds.Any())
            {
                const string locationSql = @"
INSERT INTO dbo.CompanyLocationMap
(CompanyId, LocationId, CreatedBy, CreatedDate)
VALUES
(@CompanyId, @LocationId, @CreatedBy, GETDATE());";

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

            var sessionRows = (dto.SessionTimings ?? new List<CompanySessionTimeDto>())
                .Where(x => x.SessionId > 0)
                .GroupBy(x => x.SessionId)
                .Select(g => g.First())
                .ToList();

            if (sessionRows.Any())
            {
                const string sessionMapSql = @"
INSERT INTO dbo.CompanySessionMap
(CompanyId, SessionId, CreatedBy, CreatedDate)
VALUES
(@CompanyId, @SessionId, @CreatedBy, GETDATE());";

                foreach (var session in sessionRows)
                {
                    await con.ExecuteAsync(sessionMapSql, new
                    {
                        CompanyId = companyId,
                        SessionId = session.SessionId,
                        CreatedBy = dto.UserId
                    }, tx);
                }

                const string sessionTimingSql = @"
INSERT INTO dbo.CompanySessionMapping
(CompanyId, SessionId, FromTime, ToTime, IsActive, CreatedBy, CreatedDate)
VALUES
(@CompanyId, @SessionId, @FromTime, @ToTime, 1, @CreatedBy, GETDATE());";

                foreach (var session in sessionRows)
                {
                    if (session.FromTime == default || session.ToTime == default)
                        continue;

                    await con.ExecuteAsync(sessionTimingSql, new
                    {
                        CompanyId = companyId,
                        SessionId = session.SessionId,
                        FromTime = session.FromTime,
                        ToTime = session.ToTime,
                        CreatedBy = dto.UserId
                    }, tx);
                }
            }

            if (dto.CuisineIds != null && dto.CuisineIds.Any())
            {
                const string cuisineSql = @"
INSERT INTO dbo.CompanyCuisineMap
(CompanyId, CuisineId, CreatedBy, CreatedDate)
VALUES
(@CompanyId, @CuisineId, @CreatedBy, GETDATE());";

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