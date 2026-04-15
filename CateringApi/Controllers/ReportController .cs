using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _repository;

        public ReportController(IReportRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("GetPageMasters")]
        public async Task<IActionResult> GetPageMasters([FromQuery] int userId)
        {
            var data = await _repository.GetReportPageMastersAsync(userId);
            return Ok(new { data });
        }

        [HttpPost("GetReportByDates")]
        public async Task<IActionResult> GetReportByDates([FromBody] ReportFilterDto model)
        {
            if (model.UserId <= 0)
                return BadRequest("UserId is required.");

            if (model.FromDate.HasValue && model.ToDate.HasValue && model.FromDate > model.ToDate)
                return BadRequest("From Date should not be greater than To Date.");

            var (rows, totals) = await _repository.GetReportByDatesAsync(model);

            return Ok(new
            {
                data = rows,
                foodTotals = totals
            });
        }

        [HttpPost("ExportReportExcel")]
        public async Task<IActionResult> ExportReportExcel([FromBody] ReportFilterDto model)
        {
            if (model.UserId <= 0)
                return BadRequest("UserId is required.");

            if (model.FromDate.HasValue && model.ToDate.HasValue && model.FromDate > model.ToDate)
                return BadRequest("From Date should not be greater than To Date.");

            var fileBytes = await _repository.ExportReportExcelAsync(model);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CSPL_ReportByDates_{DateTime.Now:dd-MM-yyyy}.xlsx"
            );
        }

        [HttpPost("SendReportEmail")]
        public async Task<IActionResult> SendReportEmail([FromBody] ReportEmailRequestDto model)
        {
            if (model.UserId <= 0)
                return BadRequest("UserId is required.");

            if (model.FromDate.HasValue && model.ToDate.HasValue && model.FromDate > model.ToDate)
                return BadRequest("From Date should not be greater than To Date.");

            await _repository.SendReportEmailAsync(model);
            return Ok(new { message = "Report mail sent successfully" });
        }
    }
}