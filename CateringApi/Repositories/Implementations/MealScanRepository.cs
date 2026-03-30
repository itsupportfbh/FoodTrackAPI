using CateringApi.Data;
using CateringApi.DTOs.MealScan;
using CateringApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class MealScanRepository : IMealScanRepository
    {
        private readonly DapperContext _context;

        public MealScanRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<MealScanResultDto> SaveScanAsync(MealScanSaveDto dto)
        {
            using var con = _context.CreateConnection();

            var param = new DynamicParameters();
            param.Add("@EmployeeCode", dto.EmployeeCode);
            param.Add("@MealTypeId", dto.MealTypeId);
            param.Add("@ScanTime", dto.ScanTime);
            param.Add("@QRCodeValue", dto.QRCodeValue);
            param.Add("@DeviceType", dto.DeviceType);
            param.Add("@DeviceName", dto.DeviceName);
            param.Add("@CreatedBy", dto.CreatedBy);

            var result = await con.QueryFirstAsync<MealScanResultDto>(
                "dbo.sp_SaveMealScan",
                param,
                commandType: CommandType.StoredProcedure);

            return result;
        }
    }
}