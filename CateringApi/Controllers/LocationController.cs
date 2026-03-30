using CateringApi.Data;
using CateringApi.DTOs.Common;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Location;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationRepository _repository;

        public LocationController(ILocationRepository repository) 
        { 
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLocation()
        {
            var list = await _repository.GetAllLocation();
            ResponseResult data = new ResponseResult(true, "Success", list);
            return Ok(data);
        }





        [HttpGet]
        public async Task<IActionResult> GetLocationById(int id)
        {
            var licenseObj = await _repository.GetLocationById(id);
            ResponseResult data = new ResponseResult(true, "Success", licenseObj);
            return Ok(data);
        }



        [HttpPost]
        public async Task<ActionResult> CreateLocation(LocationDTO locationDTO)
        {

            var id = await _repository.CreateLocation(locationDTO);
            ResponseResult data = new ResponseResult(true, "Location created sucessfully", id);
            return Ok(data);

        }

        [HttpPut]
        public async Task<IActionResult> UpdateLocation(LocationDTO locationDTO)
        {
            await _repository.UpdateLocation(locationDTO);
            ResponseResult data = new ResponseResult(true, "Location updated successfully.", null);
            return Ok(data);
        }



        [HttpDelete]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            await _repository.DeleteLocation(id);
            ResponseResult data = new ResponseResult(true, "Location Deleted sucessfully", null);
            return Ok(data);
        }
    }
}
