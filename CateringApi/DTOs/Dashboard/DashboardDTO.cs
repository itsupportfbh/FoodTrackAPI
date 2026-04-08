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
        public List<SessionOrderDTO> TotalOrdersBySession { get; set; }
        public List<CompanyOrderDTO> TotalcompanyWiseOrders { get; set; }
        public List<LatestQrDTO> TotallatestUsedQRs { get; set; }
    }
}
