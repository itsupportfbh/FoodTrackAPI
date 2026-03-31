using Microsoft.EntityFrameworkCore;
using CateringApi.DTOs.Master;

namespace CateringApi.Data
{
    public class FoodDBContext : DbContext
    {
        public FoodDBContext(DbContextOptions<FoodDBContext> options)
            : base(options)
        {
        }

        public DbSet<SiteSettings> SiteSettings { get; set; }
    }
}