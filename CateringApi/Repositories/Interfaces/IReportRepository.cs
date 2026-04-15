using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IReportRepository
    {
        Task<ReportPageMasterDto> GetReportPageMastersAsync(int userId);
        Task<(IEnumerable<ReportByDateRowDto> Rows, IEnumerable<FoodTotalDto> Totals)> GetReportByDatesAsync(ReportFilterDto model);
        Task<byte[]> ExportReportExcelAsync(ReportFilterDto model);
        Task SendReportEmailAsync(ReportEmailRequestDto model);
    }
}