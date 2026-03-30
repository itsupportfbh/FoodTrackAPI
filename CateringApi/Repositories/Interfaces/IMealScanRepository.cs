using CateringApi.DTOs.MealScan;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMealScanRepository
    {
        Task<MealScanResultDto> SaveScanAsync(MealScanSaveDto dto);
    }
}