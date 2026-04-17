using CateringApi.DTOs.Company;
using CateringApi.DTOs.QR;
using CateringApi.DTOs.Session;

namespace CateringApi.DTOs.Dashboard
{
    public class DashboardDTO
    {
        public int TotalCompanies { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQRCodes { get; set; }
        public int TodayScans { get; set; }
        public int YesterdayScans { get; set; }

        public decimal TodayOrderedQty { get; set; }
        public decimal TodayRedeemedQty { get; set; }
        public decimal TodayPendingQty { get; set; }

        public decimal MonthOrderedQty { get; set; }
        public decimal MonthRedeemedQty { get; set; }
        public decimal MonthPendingQty { get; set; }

        public List<SessionOrderDTO> TotalOrdersBySession { get; set; } = new();
        public List<CompanyOrderDTO> TotalcompanyWiseOrders { get; set; } = new();
        public List<LatestQrDTO> TotallatestUsedQRs { get; set; } = new();

        public List<DashboardPriceDto> CurrentSessionPrices { get; set; } = new();
    }

    public class DashboardPriceDto
    {
        public int PriceId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime EffectiveFrom { get; set; }
    }
}