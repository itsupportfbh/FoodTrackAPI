using CateringApi.DTOs.RequestOverride;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace CateringApi.Repositories.Implementations
{
    public class RequestOverrideRepository : IRequestOverrideRepository
    {
        private readonly IConfiguration _configuration;

        public RequestOverrideRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection Connection =>
            new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

       public async Task<RequestOverrideScreenDto?> GetScreenDataAsync(int requestHeaderId, DateTime fromDate, DateTime toDate)
    {
        var param = new DynamicParameters();
        param.Add("@RequestHeaderId", requestHeaderId);
        param.Add("@FromDate", fromDate.Date);
        param.Add("@ToDate", toDate.Date);

        using var multi = await Connection.QueryMultipleAsync(
            "dbo.sp_RequestOverride_GetScreenData",
            param,
            commandType: CommandType.StoredProcedure);

        var header = await multi.ReadFirstOrDefaultAsync<RequestOverrideHeaderDto>();
        var lines = (await multi.ReadAsync<RequestOverrideLineDto>()).ToList();

        if (header == null) return null;

        return new RequestOverrideScreenDto
        {
            Header = header,
            Lines = lines
        };
    }

        public async Task<SaveRequestOverrideResultDto> SaveAsync(SaveRequestOverrideDto dto)
        {
            Console.WriteLine("REPO SAVE HIT: " + DateTime.Now.ToString("HH:mm:ss.fff"));

            var param = new DynamicParameters();
            param.Add("@RequestHeaderId", dto.RequestHeaderId);
            param.Add("@FromDate", dto.FromDate.Date);
            param.Add("@ToDate", dto.ToDate.Date);
            param.Add("@Notes", dto.Notes);
            param.Add("@CreatedBy", dto.CreatedBy);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            param.Add("@LinesJson", JsonSerializer.Serialize(dto.Lines, jsonOptions));

            var result = await Connection.QueryFirstAsync<SaveRequestOverrideResultDto>(
                "dbo.sp_RequestOverride_Save",
                param,
                commandType: CommandType.StoredProcedure);

            Console.WriteLine("REPO SAVE SUCCESS ID: " + result.Id);
            Console.WriteLine("TOTAL QTY: " + result.TotalQty);
            Console.WriteLine("DIFF QTY: " + result.DifferentQty);

            return result;
        }
        public async Task<List<RequestOverrideListDto>> GetOverrideList(int companyId)
        {
            

            var result = await Connection.QueryAsync<RequestOverrideListDto>(
                "sp_RequestOverride_ListByCompany",
                new { CompanyId = companyId },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<RequestOverrideLineDto>> GetOverrideLines(int requestOverrideId)
        {
           

            var result = await Connection.QueryAsync<RequestOverrideLineDto>(
                "sp_RequestOverride_LineListByOverrideId",
                new { RequestOverrideId = requestOverrideId },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task DeleteAsync(int id, int updatedBy)
        {
            await Connection.ExecuteAsync(
                "dbo.sp_RequestOverride_Delete",
                new { Id = id, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);
        }
    }
}
