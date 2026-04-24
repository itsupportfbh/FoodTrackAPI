using CateringApi.Models;
using CateringApi.Repositories.Implementations;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequestController : ControllerBase
    {
        private readonly IRequestRepository _repository;

        public RequestController(IRequestRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("GetPageMasters")]
        public async Task<IActionResult> GetPageMasters([FromQuery] int userId)
        {
            var data = await _repository.GetPageMastersAsync(userId);
            return Ok(new { data });
        }

        [HttpGet("GetAllRequests")]
        public async Task<IActionResult> GetAllRequests([FromQuery] int userId)
        {
            var data = await _repository.GetAllRequestsAsync(userId);
            return Ok(new { data });
        }

        [HttpGet("GetRequestById/{id}")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var request = await _repository.GetRequestByIdAsync(id);
            if (request == null)
                return NotFound("Request not found");

            return Ok(new { data = request });
        }

        [HttpPost("SaveRequest")]
        public async Task<IActionResult> SaveRequest([FromBody] RequestHeaderDto model)
        {
            if (model.CompanyId <= 0)
                return BadRequest("Company is required.");

            if (model.FromDate == default)
                return BadRequest("From Date is required.");

            if (model.ToDate == default)
                return BadRequest("To Date is required.");

            if (model.FromDate.Date > model.ToDate.Date)
                return BadRequest("From Date should not be greater than ToDate.");

            if (model.Lines == null || !model.Lines.Any())
                return BadRequest("At least one line is required.");

            if (model.Lines.Any(x => string.IsNullOrWhiteSpace(x.PlanType)))
                return BadRequest("Plan is required in all lines.");

            if (model.Lines.Any(x => x.CuisineId <= 0))
                return BadRequest("Cuisine is required in all lines.");

            if (model.Lines.Any(x => x.Qty <= 0))
                return BadRequest("Qty must be greater than zero in all lines.");

            // ✅ ADD THIS BLOCK HERE
            var planUserCounts = await _repository.GetPlanUserCountsAsync(model.CompanyId);

            var userCountMap = planUserCounts.ToDictionary(
                x => x.PlanType.Trim().ToLower(),
                x => x.UserCount
            );

            var mismatchPlans = model.Lines
                .GroupBy(x => x.PlanType.Trim())
                .Select(g => new
                {
                    PlanType = g.Key,
                    EnteredQty = g.Sum(x => x.Qty),
                    AvailableUsers = userCountMap.ContainsKey(g.Key.ToLower())
                        ? userCountMap[g.Key.ToLower()]
                        : 0
                })
                .Where(x => x.EnteredQty != x.AvailableUsers)
                .ToList();

            if (mismatchPlans.Any())
            {
                var message = string.Join(" | ", mismatchPlans.Select(x =>
                    $"{x.PlanType} plan has {x.AvailableUsers} active user(s). You entered {x.EnteredQty}."));

                return BadRequest(new
                {
                    success = false,
                    message
                });
            }
            // ✅ END HERE

            var id = await _repository.SaveRequestAsync(model);

            return Ok(new
            {
                id,
                message = model.Id.HasValue && model.Id.Value > 0
                    ? "Request updated successfully"
                    : "Request created successfully"
            });
        }

        [HttpDelete("DeleteRequest/{id}")]
        public async Task<IActionResult> DeleteRequest(int id, [FromQuery] int? userId)
        {
            var deleted = await _repository.DeleteRequestAsync(id, userId);
            if (!deleted)
                return NotFound("Request not found");

            return Ok(new { message = "Request deleted successfully" });
        }

        [HttpGet("GetOrderDays")]
        public async Task<int> GetOrderDays()
        {
            var data = await _repository.GetOrderDays();
            return data;
        }

        [HttpGet("check-overlap")]
        public async Task<IActionResult> CheckOverlap(
    [FromQuery] int companyId,
    [FromQuery] DateTime fromDate,
    [FromQuery] DateTime toDate,
    [FromQuery] int id = 0)
        {
            if (companyId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid company id"
                });
            }

            if (fromDate == default || toDate == default)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "From date and To date are required"
                });
            }

            if (fromDate > toDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "From date cannot be greater than To date"
                });
            }

            var isOverlap = await _repository.CheckOverlapAsync(companyId, fromDate, toDate, id);

            return Ok(new
            {
                success = true,
                isOverlap = isOverlap,
                message = isOverlap
                    ? "Order already exists for the selected date range"
                    : "No overlap found"
            });
        }

        [HttpGet("GetPlanUserCounts")]
        public async Task<IActionResult> GetPlanUserCounts([FromQuery] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("Company is required.");

            var data = await _repository.GetPlanUserCountsAsync(companyId);
            return Ok(new { data });
        }
    }
}