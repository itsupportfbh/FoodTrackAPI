using Microsoft.EntityFrameworkCore;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Company;
using CateringApi.Models;

namespace CateringApi.Data
{
    public class FoodDBContext : DbContext
    {
        public FoodDBContext(DbContextOptions<FoodDBContext> options)
            : base(options)
        {
        }

        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<QrCodeRequest> QrCodeRequest {  get; set; }
        public DbSet<RequestHeader> Requests { get; set; }
        public DbSet<CompanyMaster> company{ get; set; }
        public DbSet<QrImage> QrImage { get; set; }
    }
}