using CateringApi.DTOModel;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] DashboardFilterDTO filter)
        {
            var data = await _dashboardRepository.GetDashboardData(filter);
            return Ok(data);
        }
    }
}
