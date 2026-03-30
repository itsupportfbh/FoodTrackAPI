using CateringApi.DTOs.Common;
using CateringApi.DTOs.Reports;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsRepository _repository;

        public ReportsController(IReportsRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("ordered-vs-scanned")]
        public async Task<IActionResult> OrderedVsScanned(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int? companyId,
            [FromQuery] int? mealTypeId)
        {
            var data = await _repository.GetOrderedVsScannedAsync(fromDate, toDate, companyId, mealTypeId);
            return Ok(ApiResponse<IEnumerable<OrderedVsScannedDto>>.Ok(data));
        }

        [HttpGet("invalid-scans")]
        public async Task<IActionResult> InvalidScans(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var data = await _repository.GetInvalidScansAsync(fromDate, toDate);
            return Ok(ApiResponse<IEnumerable<InvalidScanDto>>.Ok(data));
        }

        [HttpGet("missed-meals")]
        public async Task<IActionResult> MissedMeals(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int? companyId)
        {
            var data = await _repository.GetMissedMealsAsync(fromDate, toDate, companyId);
            return Ok(ApiResponse<IEnumerable<MissedMealDto>>.Ok(data));
        }
    }
}