using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet("GetRequestById/{requestId}")]
        public async Task<IActionResult> GetRequestById(int requestId)
        {
            var request = await _repository.GetRequestByIdAsync(requestId);
            if (request == null)
                return NotFound("Request not found");

            return Ok(new { data = request });
        }

        [HttpPost("SaveRequest")]
        public async Task<IActionResult> SaveRequest([FromBody] Request model)
        {
            if (model.CompanyId <= 0)
                return BadRequest("Company is required.");

            if (model.SessionId <= 0)
                return BadRequest("Session is required.");

            if (model.CuisineId <= 0)
                return BadRequest("Cuisine is required.");

            if (model.LocationId <= 0)
                return BadRequest("Location is required.");

            if (model.Qty <= 0)
                return BadRequest("Qty must be greater than zero.");

            if (model.FromDate == default)
                return BadRequest("From Date is required.");

            if (model.ToDate == default)
                return BadRequest("To Date is required.");

            if (model.FromDate.Date > model.ToDate.Date)
                return BadRequest("From Date should not be greater than To Date.");

            var exists = await _repository.ExistsDuplicateAsync(model);
            if (exists)
                return BadRequest("Same request already exists.");

            var id = await _repository.SaveRequestAsync(model);

            return Ok(new
            {
                requestId = id,
                message = model.RequestId.HasValue && model.RequestId.Value > 0
                    ? "Request updated successfully"
                    : "Request created successfully"
            });
        }

        [HttpDelete("DeleteRequest/{requestId}")]
        public async Task<IActionResult> DeleteRequest(int requestId, [FromQuery] int? userId)
        {
            var deleted = await _repository.DeleteRequestAsync(requestId, userId);
            if (!deleted)
                return NotFound("Request not found");

            return Ok(new { message = "Request deleted successfully" });
        }
    }
}