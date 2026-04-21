using CateringApi.DTOs;
using CateringApi.DTOs.Menu;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMenuRepository
    {
        Task<SaveMenuUploadResultDto> SaveMenuUploadAsync(SaveMenuUploadRequestDto request);
        Task<List<MenuUploadResponseDto>> GetMenuByMonthYearAsync(int month, int year);
        Task<List<MenuUploadResponseDto>> GetMenuByDateAsync(DateTime menuDate);
        Task<byte[]> GenerateMenuPdfAsync(DateTime menuDate);
        Task<byte[]> GenerateMonthlyMenuPdfAsync(int month, int year);
        Task<byte[]> DownloadPreviousMenuTemplateAsync(int month, int year);
    }
}