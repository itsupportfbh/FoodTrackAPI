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

        public decimal TotalOrderedQty { get; set; }
        public decimal TotalRedeemedQty { get; set; }
        public decimal TotalPendingQty { get; set; }

        public List<SessionOrderDTO> TotalOrdersBySession { get; set; } = new();
        public List<CompanyOrderDTO> TotalcompanyWiseOrders { get; set; } = new();
        public List<LatestQrDTO> TotallatestUsedQRs { get; set; } = new();
    }
}
