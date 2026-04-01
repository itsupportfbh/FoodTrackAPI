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
                return BadRequest("From Date should not be greater than To Date.");

            if (model.Lines == null || !model.Lines.Any())
                return BadRequest("At least one line is required.");

            if (model.Lines.Any(x => x.SessionId <= 0))
                return BadRequest("Session is required in all lines.");

            if (model.Lines.Any(x => x.CuisineId <= 0))
                return BadRequest("Cuisine is required in all lines.");

            if (model.Lines.Any(x => x.LocationId <= 0))
                return BadRequest("Location is required in all lines.");

            if (model.Lines.Any(x => x.Qty <= 0))
                return BadRequest("Qty must be greater than zero in all lines.");

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
    }
}