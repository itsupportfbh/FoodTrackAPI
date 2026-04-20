using BCrypt.Net;
using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.User;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class UserMasterRepository : IUserMasterRepository
    {
        private readonly DapperContext _context;

        public UserMasterRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserMasterDTO>> GetAllAsync(long currentUserId, int currentRoleId, int currentCompanyId)
        {
            const string sql = @"
SELECT 
    um.Id,
    um.CompanyId,
    um.RoleId,
    um.UserName,
    um.Email,
    um.PasswordHash,
    um.IsActive,
    um.CreatedBy,
    um.CreatedDate,
    um.UpdatedBy,
    um.UpdatedDate,
    um.IsDelete,
    cm.CompanyName,
    rm.RoleName
FROM UserMaster um
INNER JOIN CompanyMaster cm ON cm.Id = um.CompanyId
INNER JOIN RoleMaster rm ON rm.Id = um.RoleId
WHERE 
    (
        @CurrentRoleId = 1
        OR
        (
            @CurrentRoleId = 2
            AND um.CompanyId = @CurrentCompanyId
        )
    )
ORDER BY um.Id DESC;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<UserMasterDTO>(sql, new
            {
                CurrentUserId = currentUserId,
                CurrentRoleId = currentRoleId,
                CurrentCompanyId = currentCompanyId
            });
        }

        public async Task<UserMaster> GetByIdAsync(long id)
        {
            const string query = @"
SELECT 
    Id,
    CompanyId,
    RoleId,
    UserName,
    Email,
    PasswordHash,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate
FROM UserMaster
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<UserMaster>(query, new { Id = id });
        }

        public async Task<int> CreateAsync(CreateUserMasterDto userMaster)
        {
            if (userMaster == null)
                throw new ArgumentNullException(nameof(userMaster));

            if (string.IsNullOrWhiteSpace(userMaster.UserName))
                throw new Exception("UserName is required.");

            if (string.IsNullOrWhiteSpace(userMaster.Email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(userMaster.Password))
                throw new Exception("Password is required.");

            if (userMaster.RoleId <= 0)
                userMaster.RoleId = 2;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(userMaster.Password);
            var createdDate = DateTime.Now;
            var updatedDate = DateTime.Now;

            const string query = @"
INSERT INTO UserMaster
(
    CompanyId,
    UserName,
    Email,
    PasswordHash,
    RoleId,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate,
    IsActive,
IsDelete
)
OUTPUT INSERTED.Id
VALUES
(
    @CompanyId,
    @UserName,
    @Email,
    @PasswordHash,
    4,
    @CreatedBy,
    @CreatedDate,
    @UpdatedBy,
    @UpdatedDate,
    @IsActive,
0
);";

            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(query, new
            {
                userMaster.CompanyId,
                userMaster.UserName,
                userMaster.Email,
                PasswordHash = passwordHash,
                userMaster.RoleId,
                userMaster.CreatedBy,
                CreatedDate = createdDate,
                userMaster.UpdatedBy,
                UpdatedDate = updatedDate,
                userMaster.IsActive
            });
        }

        public async Task UpdateAsync(UserMaster userMaster)
        {
            if (userMaster == null)
                throw new ArgumentNullException(nameof(userMaster));

            if (string.IsNullOrWhiteSpace(userMaster.UserName))
                throw new Exception("UserName is required.");

            if (string.IsNullOrWhiteSpace(userMaster.Email))
                throw new Exception("Email is required.");

            if (userMaster.RoleId <= 0)
                userMaster.RoleId = 2;

            userMaster.UpdatedDate = DateTime.Now;

            string query;
            object param;

            // password new-a kudutha hash panni update pannum
            if (!string.IsNullOrWhiteSpace(userMaster.Password))
            {
                userMaster.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userMaster.Password);

                query = @"
UPDATE UserMaster
SET
    CompanyId = @CompanyId,
    UserName = @UserName,
    Email = @Email,
    PasswordHash = @PasswordHash,
    RoleId = @RoleId,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate
WHERE Id = @Id;";

                param = new
                {
                    userMaster.Id,
                    userMaster.CompanyId,
                    userMaster.UserName,
                    userMaster.Email,
                    userMaster.PasswordHash,
                    userMaster.RoleId,
                    userMaster.IsActive,
                    userMaster.UpdatedBy,
                    userMaster.UpdatedDate
                };
            }
            else
            {
                // password change illaina old hash retain
                query = @"
UPDATE UserMaster
SET
    CompanyId = @CompanyId,
    UserName = @UserName,
    Email = @Email,
    RoleId = @RoleId,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate
WHERE Id = @Id;";

                param = new
                {
                    userMaster.Id,
                    userMaster.CompanyId,
                    userMaster.UserName,
                    userMaster.Email,
                    userMaster.RoleId,
                    userMaster.IsActive,
                    userMaster.UpdatedBy,
                    userMaster.UpdatedDate
                };
            }

            using var con = _context.CreateConnection();
            await con.ExecuteAsync(query, param);
        }

        public async Task DeleteAsync(int id, int updatedBy)
        {
            const string query = @"
UPDATE UserMaster
SET
    IsDelete = 1,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate
WHERE Id = @Id;";

            using var con = _context.CreateConnection();
            await con.ExecuteAsync(query, new
            {
                Id = id,
                UpdatedBy = updatedBy,
                UpdatedDate = DateTime.Now
            });
        }



        public async Task<IEnumerable<RolesDTO>> GetRoles()
        {
            const string sql = @"
SELECT * from RoleMaster;";

            using var con = _context.CreateConnection();
            return await con.QueryAsync<RolesDTO>(sql);
        }
    }
}