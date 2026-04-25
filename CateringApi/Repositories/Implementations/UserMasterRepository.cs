using System.Data;
using BCrypt.Net;
using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.User;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace CateringApi.Repositories.Implementations
{
    public class UserMasterRepository : IUserMasterRepository
    {
        private readonly DapperContext _context;

        public UserMasterRepository(DapperContext context)
        {
            _context = context;
        }

        private static string ValidatePlanType(string? planType, int row = 0)
        {
            if (string.IsNullOrWhiteSpace(planType))
            {
                if (row > 0)
                    throw new Exception($"Row {row}: Plan Type is required.");

                throw new Exception("Plan Type is required.");
            }

            var validPlanTypes = new[] { "Basic", "Standard", "Premium" };

            var matchedPlanType = validPlanTypes.FirstOrDefault(x =>
                x.Equals(planType.Trim(), StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(matchedPlanType))
            {
                if (row > 0)
                    throw new Exception($"Row {row}: Plan Type must be Basic, Standard, or Premium.");

                throw new Exception("Plan Type must be Basic, Standard, or Premium.");
            }

            return matchedPlanType;
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
    um.PlanType,
    cm.CompanyName,
    cm.CompanyCode,
    rm.RoleName,
    um.CuisineId,
    cu.CuisineName
FROM UserMaster um
INNER JOIN CompanyMaster cm ON cm.Id = um.CompanyId
INNER JOIN RoleMaster rm ON rm.Id = um.RoleId
LEFT JOIN CuisineMaster cu ON cu.Id = um.CuisineId
WHERE ISNULL(um.IsDelete, 0) = 0
AND
(
    @CurrentRoleId = 1
    OR (@CurrentRoleId = 2 AND um.CompanyId = @CurrentCompanyId)
    OR (@CurrentRoleId = 4 AND um.Id = @CurrentUserId)
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
    UpdatedDate,
    IsDelete,
    PlanType,
    CuisineId
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

            if (userMaster.CompanyId <= 0)
                throw new Exception("Company is required.");

            if (userMaster.RoleId <= 0)
                userMaster.RoleId = 4;

            userMaster.PlanType = ValidatePlanType(userMaster.PlanType);

            using var con = _context.CreateConnection();

            var duplicateCheck = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM UserMaster
WHERE ISNULL(IsDelete, 0) = 0
AND (
    LOWER(UserName) = LOWER(@UserName)
    OR LOWER(Email) = LOWER(@Email)
);", new
            {
                userMaster.UserName,
                userMaster.Email
            });

            if (duplicateCheck > 0)
                throw new Exception("UserName or Email already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(userMaster.Password);
            var createdDate = DateTime.Now;
            var updatedDate = DateTime.Now;

            const string query = @"
INSERT INTO UserMaster
(
    CompanyId,
    RoleId,
    PlanType,
    CuisineId,
    UserName,
    Email,
    PasswordHash,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate,
    IsDelete
)
OUTPUT INSERTED.Id
VALUES
(
    @CompanyId,
    @RoleId,
    @PlanType,
    @CuisineId,
    @UserName,
    @Email,
    @PasswordHash,
    @IsActive,
    @CreatedBy,
    @CreatedDate,
    @UpdatedBy,
    @UpdatedDate,
    0
);";

            return await con.ExecuteScalarAsync<int>(query, new
            {
                userMaster.CompanyId,
                userMaster.RoleId,
                userMaster.PlanType,
                userMaster.CuisineId,
                userMaster.UserName,
                userMaster.Email,
                PasswordHash = passwordHash,
                userMaster.IsActive,
                userMaster.CreatedBy,
                CreatedDate = createdDate,
                userMaster.UpdatedBy,
                UpdatedDate = updatedDate
            });
        }

        public async Task UpdateAsync(UserMaster1 userMaster)
        {
            if (userMaster == null)
                throw new ArgumentNullException(nameof(userMaster));

            if (string.IsNullOrWhiteSpace(userMaster.UserName))
                throw new Exception("UserName is required.");

            if (string.IsNullOrWhiteSpace(userMaster.Email))
                throw new Exception("Email is required.");

            if (userMaster.CompanyId <= 0)
                throw new Exception("Company is required.");

            if (userMaster.RoleId <= 0)
                userMaster.RoleId = 4;

            userMaster.PlanType = ValidatePlanType(userMaster.PlanType);

            using var con = _context.CreateConnection();

            var duplicateCheck = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM UserMaster
WHERE ISNULL(IsDelete, 0) = 0
AND Id <> @Id
AND (
    LOWER(UserName) = LOWER(@UserName)
    OR LOWER(Email) = LOWER(@Email)
);", new
            {
                userMaster.Id,
                userMaster.UserName,
                userMaster.Email
            });

            if (duplicateCheck > 0)
                throw new Exception("UserName or Email already exists.");

            userMaster.UpdatedDate = DateTime.Now;

            string query;
            object param;

            if (!string.IsNullOrWhiteSpace(userMaster.Password1))
            {
                userMaster.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userMaster.Password1);

                query = @"
UPDATE UserMaster
SET
    CompanyId = @CompanyId,
    RoleId = @RoleId,
    PlanType = @PlanType,
    CuisineId = @CuisineId,
    UserName = @UserName,
    Email = @Email,
    PasswordHash = @PasswordHash,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate
WHERE Id = @Id;";

                param = new
                {
                    userMaster.Id,
                    userMaster.CompanyId,
                    userMaster.RoleId,
                    userMaster.PlanType,
                    userMaster.CuisineId,
                    userMaster.UserName,
                    userMaster.Email,
                    userMaster.PasswordHash,
                    userMaster.IsActive,
                    userMaster.UpdatedBy,
                    userMaster.UpdatedDate
                };
            }
            else
            {
                query = @"
UPDATE UserMaster
SET
    CompanyId = @CompanyId,
    RoleId = @RoleId,
    PlanType = @PlanType,
    UserName = @UserName,
    Email = @Email,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate
WHERE Id = @Id;";

                param = new
                {
                    userMaster.Id,
                    userMaster.CompanyId,
                    userMaster.RoleId,
                    userMaster.PlanType,
                    userMaster.UserName,
                    userMaster.Email,
                    userMaster.IsActive,
                    userMaster.UpdatedBy,
                    userMaster.UpdatedDate
                };
            }

            await con.ExecuteAsync(query, param);
        }

        public async Task DeleteAsync(int id, int updatedBy)
        {
            const string query = @"
UPDATE UserMaster
SET
    IsDelete = 1,
    IsActive = 0,
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
            const string sql = @"SELECT * FROM RoleMaster;";

            using var con = _context.CreateConnection();

            return await con.QueryAsync<RolesDTO>(sql);
        }

        public async Task<byte[]> DownloadUserTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("FBH Group");

            using var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add("Users");
            var planSheet = package.Workbook.Worksheets.Add("PlanTypes");

            planSheet.Hidden = eWorkSheetHidden.VeryHidden;

            worksheet.Cells[1, 1].Value = "UserName";
            worksheet.Cells[1, 2].Value = "Email";
            worksheet.Cells[1, 3].Value = "Password";
            worksheet.Cells[1, 4].Value = "IsActive";
            worksheet.Cells[1, 5].Value = "PlanType";

            worksheet.Cells[2, 1].Value = "John Peter";
            worksheet.Cells[2, 2].Value = "john@company.com";
            worksheet.Cells[2, 3].Value = "123456";
            worksheet.Cells[2, 4].Value = "TRUE";
            worksheet.Cells[2, 5].Value = "Basic";

            planSheet.Cells[1, 1].Value = "Basic";
            planSheet.Cells[2, 1].Value = "Standard";
            planSheet.Cells[3, 1].Value = "Premium";

            var validation = worksheet.DataValidations.AddListValidation("E2:E1000");
            validation.Formula.ExcelFormula = "PlanTypes!$A$1:$A$3";
            validation.ShowErrorMessage = true;
            validation.ErrorTitle = "Invalid Plan Type";
            validation.Error = "Please select Basic, Standard, or Premium.";

            worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
            worksheet.Cells.AutoFitColumns();

            return await Task.FromResult(package.GetAsByteArray());
        }

        public async Task<string> BulkUploadUsersAsync(IFormFile file, int updatedBy, int companyId)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Please upload a valid Excel file.");

            if (companyId <= 0)
                throw new Exception("Invalid company id from login user.");

            ExcelPackage.License.SetNonCommercialPersonal("FBH Group");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
                throw new Exception("Worksheet not found or empty.");

            using var con = _context.CreateConnection();

            if (con.State != ConnectionState.Open)
                con.Open();

            int insertedCount = 0;
            int updatedCount = 0;
            int defaultRoleId = 4;

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var userName = worksheet.Cells[row, 1].Text?.Trim();
                var email = worksheet.Cells[row, 2].Text?.Trim();
                var password = worksheet.Cells[row, 3].Text?.Trim();
                var isActiveText = worksheet.Cells[row, 4].Text?.Trim();
                var planTypeText = worksheet.Cells[row, 5].Text?.Trim();

                if (
                    string.IsNullOrWhiteSpace(userName) &&
                    string.IsNullOrWhiteSpace(email) &&
                    string.IsNullOrWhiteSpace(planTypeText)
                )
                    continue;

                if (string.IsNullOrWhiteSpace(userName))
                    throw new Exception($"Row {row}: UserName is required.");

                if (string.IsNullOrWhiteSpace(email))
                    throw new Exception($"Row {row}: Email is required.");

                var planType = ValidatePlanType(planTypeText, row);

                bool isActive = true;

                if (!string.IsNullOrWhiteSpace(isActiveText))
                    bool.TryParse(isActiveText, out isActive);

                var existingUser = await con.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 Id
FROM UserMaster
WHERE ISNULL(IsDelete, 0) = 0
AND LOWER(Email) = LOWER(@Email);", new
                {
                    Email = email
                });

                if (existingUser != null)
                {
                    string updateQuery = @"
UPDATE UserMaster
SET
    UserName = @UserName,
    CompanyId = @CompanyId,
    RoleId = @RoleId,
    PlanType = @PlanType,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = @UpdatedDate"
                        + (!string.IsNullOrWhiteSpace(password) ? ", PasswordHash = @PasswordHash " : " ")
                        + @"
WHERE Id = @Id;";

                    await con.ExecuteAsync(updateQuery, new
                    {
                        Id = (int)existingUser.Id,
                        UserName = userName,
                        CompanyId = companyId,
                        RoleId = defaultRoleId,
                        PlanType = planType,
                        IsActive = isActive,
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now,
                        PasswordHash = !string.IsNullOrWhiteSpace(password)
                            ? BCrypt.Net.BCrypt.HashPassword(password)
                            : null
                    });

                    updatedCount++;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(password))
                        password = "123456";

                    const string insertQuery = @"
INSERT INTO UserMaster
(
    CompanyId,
    RoleId,
    PlanType,
    UserName,
    Email,
    PasswordHash,
    IsActive,
    CreatedBy,
    CreatedDate,
    UpdatedBy,
    UpdatedDate,
    IsDelete
)
VALUES
(
    @CompanyId,
    @RoleId,
    @PlanType,
    @UserName,
    @Email,
    @PasswordHash,
    @IsActive,
    @CreatedBy,
    @CreatedDate,
    @UpdatedBy,
    @UpdatedDate,
    0
);";

                    await con.ExecuteAsync(insertQuery, new
                    {
                        CompanyId = companyId,
                        RoleId = defaultRoleId,
                        PlanType = planType,
                        UserName = userName,
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        IsActive = isActive,
                        CreatedBy = updatedBy,
                        CreatedDate = DateTime.Now,
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now
                    });

                    insertedCount++;
                }
            }

            return $"Bulk upload completed successfully. Inserted: {insertedCount}, Updated: {updatedCount}";
        }
    }
}