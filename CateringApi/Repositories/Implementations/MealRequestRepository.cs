using CateringApi.DTOs.MealPlan;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class MealRequestRepository : IMealRequestRepository
    {
        private readonly IConfiguration _configuration;

        public MealRequestRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<MealRequestListDto>> GetAllMealRequests(int companyId, int userId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT
                    Id,
                    CompanyId,
                    UserId,
                    FromDate,
                    ToDate,
                    LocationId,
                    CreatedDate
                FROM MealRequest
                WHERE CompanyId = @CompanyId
                  AND UserId = @UserId
                  AND IsActive = 1
                ORDER BY FromDate ASC;
            ";

            return await connection.QueryAsync<MealRequestListDto>(sql, new
            {
                CompanyId = companyId,
                UserId = userId
            });
        }

        public async Task<MealRequestListDto?> GetMealRequestById(int id)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT
                    Id,
                    CompanyId,
                    UserId,
                    FromDate,
                    ToDate,
                    LocationId,
                    CreatedDate
                FROM MealRequest
                WHERE Id = @Id
                  AND IsActive = 1;
            ";

            return await connection.QueryFirstOrDefaultAsync<MealRequestListDto>(sql, new { Id = id });
        }

        public async Task<object> SaveMealRequest(SaveMealRequestDto dto)
        {
            using var connection = CreateConnection();

            if (dto.CompanyId <= 0)
                return new { status = false, message = "Company is required.", data = (object?)null };

            if (dto.UserId <= 0)
                return new { status = false, message = "User is required.", data = (object?)null };

            if (dto.LocationId <= 0)
                return new { status = false, message = "Location is required.", data = (object?)null };

            if (dto.FromDate.Date > dto.ToDate.Date)
                return new { status = false, message = "From date should not be greater than To date.", data = (object?)null };

            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var sql = @"
                    DECLARE @OldRows TABLE
                    (
                        Id INT,
                        FromDate DATE,
                        ToDate DATE,
                        LocationId INT
                    );

                    INSERT INTO @OldRows
                    (
                        Id,
                        FromDate,
                        ToDate,
                        LocationId
                    )
                    SELECT
                        Id,
                        CAST(FromDate AS DATE),
                        CAST(ToDate AS DATE),
                        LocationId
                    FROM MealRequest
                    WHERE CompanyId = @CompanyId
                      AND UserId = @UserId
                      AND IsActive = 1
                      AND @FromDate <= CAST(ToDate AS DATE)
                      AND @ToDate >= CAST(FromDate AS DATE);

                    UPDATE MR
                    SET
                        IsActive = 0,
                        UpdatedBy = @UpdatedBy,
                        UpdatedDate = GETDATE()
                    FROM MealRequest MR
                    INNER JOIN @OldRows O ON O.Id = MR.Id;

                    INSERT INTO MealRequest
                    (
                        CompanyId,
                        UserId,
                        FromDate,
                        ToDate,
                        LocationId,
                        CreatedBy,
                        CreatedDate,
                        IsActive
                    )
                    SELECT
                        @CompanyId,
                        @UserId,
                        O.FromDate,
                        DATEADD(DAY, -1, @FromDate),
                        O.LocationId,
                        @CreatedBy,
                        GETDATE(),
                        1
                    FROM @OldRows O
                    WHERE O.FromDate < @FromDate;

                    INSERT INTO MealRequest
                    (
                        CompanyId,
                        UserId,
                        FromDate,
                        ToDate,
                        LocationId,
                        CreatedBy,
                        CreatedDate,
                        IsActive
                    )
                    VALUES
                    (
                        @CompanyId,
                        @UserId,
                        @FromDate,
                        @ToDate,
                        @LocationId,
                        @CreatedBy,
                        GETDATE(),
                        1
                    );

                    DECLARE @NewId INT = CAST(SCOPE_IDENTITY() AS INT);

                    INSERT INTO MealRequest
                    (
                        CompanyId,
                        UserId,
                        FromDate,
                        ToDate,
                        LocationId,
                        CreatedBy,
                        CreatedDate,
                        IsActive
                    )
                    SELECT
                        @CompanyId,
                        @UserId,
                        DATEADD(DAY, 1, @ToDate),
                        O.ToDate,
                        O.LocationId,
                        @CreatedBy,
                        GETDATE(),
                        1
                    FROM @OldRows O
                    WHERE O.ToDate > @ToDate;

                    SELECT @NewId;
                ";

                var newId = await connection.ExecuteScalarAsync<int>(
                    sql,
                    new
                    {
                        dto.CompanyId,
                        dto.UserId,
                        FromDate = dto.FromDate.Date,
                        ToDate = dto.ToDate.Date,
                        dto.LocationId,
                        dto.CreatedBy,
                        UpdatedBy = dto.UpdatedBy ?? dto.CreatedBy
                    },
                    transaction
                );

                transaction.Commit();

                return new
                {
                    status = true,
                    message = "Meal request saved successfully.",
                    data = newId
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                return new
                {
                    status = false,
                    message = ex.Message,
                    data = (object?)null
                };
            }
        }

        public async Task<object> DeleteMealRequest(int id)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MealRequest
                SET
                    IsActive = 0,
                    UpdatedDate = GETDATE()
                WHERE Id = @Id
                  AND IsActive = 1;
            ";

            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });

            if (affectedRows == 0)
            {
                return new
                {
                    status = false,
                    message = "Meal request not found.",
                    data = (object?)null
                };
            }

            return new
            {
                status = true,
                message = "Meal request deleted successfully.",
                data = id
            };
        }


        public async Task<IEnumerable<ShowQrDTO>> ShowQr(int companyId, int userId)
        {
            using var connection = CreateConnection();

            var sql = @"
SELECT
    qi.Id AS QrImageId,
    qi.QrCodeImage,
    qi.QrCodeText,
    qi.PlanType
FROM QrUserAssignment qua
INNER JOIN QrImage qi ON qi.Id = qua.QrImageId
WHERE qua.CompanyId = @CompanyId
  AND qua.UserId = @UserId;
            ";

            return await connection.QueryAsync<ShowQrDTO>(sql, new
            {
                CompanyId = companyId,
                UserId = userId
            });
        }
    }
}