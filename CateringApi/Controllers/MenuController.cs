using CateringApi.DTOs;
using CateringApi.DTOs.Menu;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMenuRepository _repository;

        public MenuController(IMenuRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveMenuUpload([FromBody] SaveMenuUploadRequestDto request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            if (request.MenuMonth <= 0 || request.MenuMonth > 12)
                return BadRequest(new { success = false, message = "Invalid month." });

            if (request.MenuYear <= 0)
                return BadRequest(new { success = false, message = "Invalid year." });

            if (request.Rows == null || !request.Rows.Any())
                return BadRequest(new { success = false, message = "No rows found." });

            var result = await _repository.SaveMenuUploadAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetMenuByMonthYear([FromQuery] int month, [FromQuery] int year)
        {
            if (month <= 0 || month > 12)
                return BadRequest(new { success = false, message = "Invalid month." });

            if (year <= 0)
                return BadRequest(new { success = false, message = "Invalid year." });

            var result = await _repository.GetMenuByMonthYearAsync(month, year);
            return Ok(result);
        }

        [HttpGet("by-month-year")]
        public async Task<IActionResult> GetByMonthYear(int month, int year)
        {
            var result = await _repository.GetMenuByMonthYearAsync(month, year);
            return Ok(result);
        }

        [HttpGet("by-date")]
        public async Task<IActionResult> GetByDate(DateTime menuDate)
        {
            var result = await _repository.GetMenuByDateAsync(menuDate);
            return Ok(result);
        }

        [HttpGet("download-pdf")]
        public async Task<IActionResult> DownloadMenuPdf(DateTime menuDate)
        {
            var pdf = await _repository.GenerateMenuPdfAsync(menuDate);
            var fileName = $"Menu_{menuDate:yyyyMMdd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet("download-monthly-pdf")]
        public async Task<IActionResult> DownloadMonthlyMenuPdf(int month, int year)
        {
            var pdfBytes = await _repository.GenerateMonthlyMenuPdfAsync(month, year);
            var fileName = $"Monthly_Menu_{year}_{month:D2}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("download-previous-template")]
        public async Task<IActionResult> DownloadPreviousMenuTemplate([FromQuery] int month, [FromQuery] int year)
        {
            if (month <= 0 || month > 12)
                return BadRequest(new { success = false, message = "Invalid month." });

            if (year <= 0)
                return BadRequest(new { success = false, message = "Invalid year." });

            var fileBytes = await _repository.DownloadPreviousMenuTemplateAsync(month, year);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No previously uploaded menu Excel found for selected month and year."
                });
            }

            var monthName = new DateTime(year, month, 1).ToString("MMM");
            var fileName = $"previous_menu_{monthName}_{year}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}