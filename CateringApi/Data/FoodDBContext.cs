using CateringApi.DTOModel;
using CateringApi.DTOs;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Request;
using CateringApi.DTOs.RequestOverride;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CateringApi.Data
{
    public class FoodDBContext : DbContext
    {
        public FoodDBContext(DbContextOptions<FoodDBContext> options)
            : base(options)
        {
        }

        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<QrCodeRequest> QrCodeRequest { get; set; }
        public DbSet<RequestHeader> RequestHeader { get; set; }
        public DbSet<RequestDetail> RequestDetail { get; set; }
        public DbSet<CompanyMaster> CompanyMaster { get; set; }
        public DbSet<Session> Session { get; set; }
        public DbSet<QrImage> QrImage { get; set; }
        public DbSet<RequestOverride> RequestOverride { get; set; }
        public DbSet<RequestOverrideDetail> RequestOverrideDetail { get; set; }
        public DbSet<QrScanLog> QrScanLog { get; set; }
        public DbSet<CompanySessionMapping> CompanySessionMapping { get; set; }
        public DbSet<CuisinePriceHistoryDto> SessionPriceHistory { get; set; }
        public DbSet<Cuisine> CuisineMaster { get; set; }
        public DbSet<PriceListDto> SessionPrice { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CompanySessionMapping>(entity =>
            {
                entity.ToTable("CompanySessionMapping");
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<RequestOverride>(entity =>
            {
                entity.ToTable("RequestOverride");
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<RequestOverrideDetail>(entity =>
            {
                entity.ToTable("RequestOverrideDetail");
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.RequestOverride)
                      .WithMany(x => x.RequestOverrideDetails)
                      .HasForeignKey(x => x.RequestOverrideId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}