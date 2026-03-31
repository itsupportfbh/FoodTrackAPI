using CateringApi.DTOs.Company;
using CateringApi.DTOs.Master;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CateringApi.Repositories.Interfaces
{
    public interface ISiteSettingsRepository
    {


           public List<SiteSettings> GetAllSiteSettings();
            Task<SiteSettings> GetSiteSettingsbyid(int id);
           public  Task<SiteSettings> AddUpdateSiteSettings(SiteSettings model);
        public SiteSettings DeleteSiteSettings(int id, int? userId);
        public  Task<SiteSettings?> GetLatestSiteSetting();


    }
}
