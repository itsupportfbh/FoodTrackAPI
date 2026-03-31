using CateringApi.DTOs.Master;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SiteSettingsController : ControllerBase
    {
        private readonly ISiteSettingsRepository _siteSettingsRepository;
        public SiteSettingsController(ISiteSettingsRepository siteSettingsRepository)
        {
            _siteSettingsRepository = siteSettingsRepository;
        }
        [HttpGet]
        public async Task<SiteSettings?> GetSitesettingsbyid(int id)
        {
            var result = await _siteSettingsRepository.GetSiteSettingsbyid(id);

            if (result == null)
                return new SiteSettings();
            else

                return result;
        }
        [HttpGet]
        public List<SiteSettings> GetAllSiteSettings()
        {
            var result=_siteSettingsRepository.GetAllSiteSettings();
            return result;
        }
        [HttpPost]
        public Task<SiteSettings> AddUpdateSiteSettings(SiteSettings model)
        {
            var result = _siteSettingsRepository.AddUpdateSiteSettings(model);
            return result;
     
        }
        [HttpDelete]
        public SiteSettings DeleteSiteSettings(int id, int? userId)
        {
            var result = _siteSettingsRepository.DeleteSiteSettings(id, userId);
            return result;
        }
        [HttpGet]
        public async Task<SiteSettings?> GetLatestSiteSetting()
        {
            return await _siteSettingsRepository.GetLatestSiteSetting();
        }
    }
}
