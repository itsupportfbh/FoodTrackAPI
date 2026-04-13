using CateringApi.DTOs.Master;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteSettingsController : ControllerBase
    {
        private readonly ISiteSettingsRepository _siteSettingsRepository;
        public SiteSettingsController(ISiteSettingsRepository siteSettingsRepository)
        {
            _siteSettingsRepository = siteSettingsRepository;
        }
        [HttpGet("GetSitesettingsbyid")]
        public async Task<SiteSettings?> GetSitesettingsbyid(int id)
        {
            var result = await _siteSettingsRepository.GetSiteSettingsbyid(id);

            if (result == null)
                return new SiteSettings();
            else

                return result;
        }
        [HttpGet("GetAllSiteSettings")]
        public List<SiteSettings> GetAllSiteSettings()
        {
            var result=_siteSettingsRepository.GetAllSiteSettings();
            return result;
        }
        [HttpPost("AddUpdateSiteSettings")]
        public Task<SiteSettings> AddUpdateSiteSettings(SiteSettings model)
        {
            var result = _siteSettingsRepository.AddUpdateSiteSettings(model);
            return result;
     
        }
        [HttpDelete("DeleteSiteSettings")]
        public SiteSettings DeleteSiteSettings(int id, int? userId)
        {
            var result = _siteSettingsRepository.DeleteSiteSettings(id, userId);
            return result;
        }
        [HttpGet]
        public async Task<IActionResult> GetLatestSiteSetting()
        {
            var setting = await _siteSettingsRepository.GetLatestSiteSetting();

            if (setting == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Site settings not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = setting
            });
        }
    }
}
