using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuisineController : ControllerBase
    {
        private readonly ICuisineRepository _repository;

        public CuisineController(ICuisineRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("GetCuisines")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repository.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("GetCuisineById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cuisine = await _repository.GetByIdAsync(id);
            if (cuisine == null)
                return NotFound("Cuisine not found");

            return Ok(cuisine);
        }

        [HttpPost("SaveCuisine")]
        public async Task<IActionResult> Save([FromBody] Cuisine model)
        {
            if (string.IsNullOrWhiteSpace(model.CuisineName))
                return BadRequest("Cuisine name is required.");

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                var exists = await _repository.NameExistsAsync(model.CuisineName, model.Id.Value);
                if (exists)
                    return BadRequest("Cuisine already exists.");

                var id = await _repository.SaveAsync(model);
                return Ok(new
                {
                    id,
                    message = "Cuisine updated successfully"
                });
            }
            else
            {
                var existing = await _repository.GetByNameAsync(model.CuisineName);
                if (existing != null)
                    return BadRequest("Cuisine already exists.");

                var id = await _repository.SaveAsync(model);
                return Ok(new
                {
                    id,
                    message = "Cuisine created successfully"
                });
            }
        }

        [HttpDelete("DeleteCuisine/{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
        {
            var deleted = await _repository.DeleteAsync(id, userId);
            if (!deleted)
                return NotFound("Cuisine not found");

            return Ok(new { message = "Cuisine deleted successfully" });
        }
    }
}