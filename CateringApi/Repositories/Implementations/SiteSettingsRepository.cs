using CateringApi.Data;
using CateringApi.DTOs.Master;
using CateringApi.NewFolder;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CateringApi.Repositories.Implementations
{
    public class SiteSettingsRepository : ISiteSettingsRepository
    {
        private readonly FoodDBContext _context;

        public SiteSettingsRepository(FoodDBContext context)
        {
            _context = context;
        }
        public async Task<bool> DeleteSitesettingsbyId(int id, int? userId)
        {
            var entity = await _context.SiteSettings
       .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            entity.IsActive = false;
            entity.UpdatedBy = userId ?? 0;
            entity.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;

        }

        public List<SiteSettings> GetAllSiteSettings()
        {
            var result = _context.SiteSettings.Where(x=>x.IsActive==true).ToList();
            return result;
        }
               

        public SiteSettings DeleteSiteSettings(int id, int? userId)
        {
            var result = _context.SiteSettings
                .FirstOrDefault(x => x.Id == id&& x.IsActive == true);

            if (result != null)
            {
                result.IsActive = false;
                result.UpdatedDate = DateTime.Now;
                result.UpdatedBy = userId ?? 0;

                _context.SiteSettings.Update(result);
                _context.SaveChanges();

                return result;
            }

            return new SiteSettings();
        }

        public async Task<SiteSettings> AddUpdateSiteSettings(SiteSettings model)
        {
            var existing = await _context.SiteSettings
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.IsActive == true);

            if (existing == null)
            {
                var entity = new SiteSettings
                {
                    BreakfastCutOffTime = model.BreakfastCutOffTime,
                    LunchCutOffTime = model.LunchCutOffTime,
                    LateLunchCutOffTime = model.LateLunchCutOffTime,
                    DinnerCutOffTime = model.DinnerCutOffTime,
                    LateDinnerCutOffTime = model.LateDinnerCutOffTime,
                    orderDays = model.orderDays,
                    CronEmail = model.CronEmail,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = model.CreatedBy,
                    UpdatedBy = model.UpdatedBy
                };

                await _context.SiteSettings.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            else
            {
                existing.BreakfastCutOffTime = model.BreakfastCutOffTime;
                existing.LunchCutOffTime = model.LunchCutOffTime;
                existing.LateLunchCutOffTime = model.LateLunchCutOffTime;
                existing.DinnerCutOffTime = model.DinnerCutOffTime;
                existing.LateDinnerCutOffTime = model.LateDinnerCutOffTime;
                existing.orderDays = model.orderDays;
                existing.CronEmail = model.CronEmail;
                existing.UpdatedDate = DateTime.Now;
                existing.UpdatedBy = model.UpdatedBy;

                await _context.SaveChangesAsync();
                return existing;
            }
        }

        public Task<SiteSettings> GetSiteSettingsbyid(int id)
        {

            var result = _context.SiteSettings.Where(x => x.IsActive == true && x.Id == id).FirstOrDefaultAsync();
            return result;
        }

        public async Task<SiteSettings?> GetLatestSiteSetting()
        {
            return await _context.SiteSettings
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                .FirstOrDefaultAsync();
        }
    }
}
