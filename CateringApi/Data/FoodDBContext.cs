using CateringApi.DTOModel;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Item;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Request;
using CateringApi.DTOs.RequestOverride;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

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
        public DbSet<RequestHeader> RequestHeader { get; set; }
        public DbSet<RequestDetail> RequestDetail { get; set; }
        public DbSet<CompanyMaster>CompanyMaster { get; set; }
        //public DbSet<SessionDTO> Sessions { get; set; }
        public DbSet<Session> Session { get; set; }
        public DbSet<QrImage> QrImage { get; set; }
        public DbSet<RequestOverride> RequestOverride { get; set; }
       
        public DbSet<RequestOverrideLineDto> RequestOverrideDetail { get; set; }    
        public DbSet<QrScanLog> QrScanLog { get; set; }

    }
}