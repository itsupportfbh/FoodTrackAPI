using CateringApi.DTOs;
using CateringApi.DTOs.Menu;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMenuRepository
    {
        Task<SaveMenuUploadResultDto> SaveMenuUploadAsync(SaveMenuUploadRequestDto request);
        Task<List<MenuUploadResponseDto>> GetMenuByMonthYearAsync(int month, int year);
    }
}