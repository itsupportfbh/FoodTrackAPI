using CateringApi.DTOs;
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

        [HttpGet("GetAllSessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            try
            {
                var data = await _repository.GetAllSessionsAsync();
                return Ok(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("GetDefaultPlanRates")]
        public async Task<IActionResult> GetDefaultPlanRates()
        {
            try
            {
                var result = await _repository.GetDefaultPlanRatesAsync();
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("SaveDefaultPlanRatesBulk")]
        public async Task<IActionResult> SaveDefaultPlanRatesBulk([FromBody] DefaultPlanBulkSaveRequest request)
        {
            if (request == null || request.Plans == null || !request.Plans.Any())
                return BadRequest(new { message = "Invalid request" });

            var allowedPlans = new[] { "Basic", "Standard", "Premium" };

            foreach (var plan in request.Plans)
            {
                if (string.IsNullOrWhiteSpace(plan.PlanType))
                    return BadRequest(new { message = "Plan type is required" });

                if (!allowedPlans.Contains(plan.PlanType))
                    return BadRequest(new { message = $"Invalid plan type: {plan.PlanType}" });

                if (plan.SessionRates == null || !plan.SessionRates.Any())
                    return BadRequest(new { message = $"Session rates are required for {plan.PlanType}" });

                if (plan.SessionRates.Any(x => x.Rate <= 0))
                    return BadRequest(new { message = $"All rates must be greater than zero for {plan.PlanType}" });
            }

            var result = await _repository.SaveDefaultPlanRatesBulkAsync(request);

            return Ok(new
            {
                success = result,
                message = result ? "Default plan rates saved successfully" : "Save failed"
            });
        }

        [HttpGet("GetPriceList")]
        public async Task<IActionResult> GetPriceList()
        {
            var result = await _repository.GetPriceList();
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int sessionId, [FromQuery] string planType)
        {
            try
            {
                var data = await _repository.GetDefaultPriceHistoryAsync(sessionId, planType);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}