using CateringApi.DTOs;
using CateringApi.Repositories.Implementations;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CuisinePriceController : ControllerBase
    {
        private readonly ICuisinePriceRepository _repository;

        public CuisinePriceController(ICuisinePriceRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("all-cuisines-with-rates")]
        public async Task<IActionResult> GetAllCuisinesWithRates([FromQuery] int companyId, [FromQuery] int sessionId)
        {
            try
            {
                var data = await _repository.GetAllCuisinesWithRatesAsync(companyId, sessionId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("save-bulk")]
        public async Task<IActionResult> SaveSessionRate([FromBody] SessionRateSaveRequest request)
        {
            var result = await _repository.SaveSessionRateAsync(request);

            return Ok(new
            {
                success = result,
                message = "Session price saved successfully"
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int companyId, [FromQuery] int sessionId, [FromQuery] int cuisineId)
        {
            try
            {
                var data = await _repository.GetCuisinePriceHistoryAsync(companyId, sessionId, cuisineId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet("GetPriceList")]
        public async Task<IActionResult> GetPriceList()
        {
            var result = await _repository.GetPriceList();
            return Ok(result);
        }

        [HttpGet("GetAssignedSessionsByCompanyId/{companyId}")]
        public async Task<IActionResult> GetAssignedSessionsByCompanyId(int companyId)
        {
            try
            {
                var data = await _repository.GetAssignedSessionsByCompanyIdAsync(companyId);
                return Ok(new
                {
                    status = true,
                    data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet("GetCompanyPlanRates")]
        public async Task<IActionResult> GetCompanyPlanRates(int companyId)
        {
            var result = await _repository.GetCompanyPlanRatesAsync(companyId);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("SaveCompanyPlanRates")]
        public async Task<IActionResult> SaveCompanyPlanRates([FromBody] CompanyPlanRateSaveRequest request)
        {
            if (request == null || request.CompanyId <= 0)
                return BadRequest(new { message = "Invalid company" });

            if (string.IsNullOrWhiteSpace(request.PlanType))
                return BadRequest(new { message = "Plan type is required" });

            if (request.SessionRates == null || !request.SessionRates.Any())
                return BadRequest(new { message = "Session rates are required" });

            var allowedPlans = new[] { "Basic", "Standard", "Premium" };
            if (!allowedPlans.Contains(request.PlanType))
                return BadRequest(new { message = "Invalid plan type" });

            var invalidRate = request.SessionRates.FirstOrDefault(x => x.Rate <= 0);
            if (invalidRate != null)
                return BadRequest(new { message = "All rates must be greater than zero" });

            var result = await _repository.SaveCompanyPlanRatesAsync(request);

            return Ok(new
            {
                success = result,
                message = result ? "Plan rates saved successfully" : "Save failed"
            });
        }
    }
}