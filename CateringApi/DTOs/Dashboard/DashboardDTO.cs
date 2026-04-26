using CateringApi.DTOs.Company;
using CateringApi.DTOs.QR;
using CateringApi.DTOs.Session;
using static CateringApi.Repositories.Implementations.DashboardRepository;

namespace CateringApi.DTOs.Dashboard
{
    public class DashboardDTO
    {
        public int TotalCompanies { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQRCodes { get; set; }
        public int TodayScans { get; set; }
        public int YesterdayScans { get; set; }

        public int TodayOrderedQty { get; set; }
        public int TodayRedeemedQty { get; set; }
        public int TodayPendingQty { get; set; }

        public int MonthOrderedQty { get; set; }
        public int MonthRedeemedQty { get; set; }
        public int MonthPendingQty { get; set; }

        public decimal TotalPrice { get; set; }
        public bool IsOverrideApplied { get; set; }

        public List<PlanTypeQtyDTO> TotalOrdersByPlanType { get; set; } = new();
        public List<SessionQtyDTO> TotalOrdersBySession { get; set; } = new();
        public List<SessionPriceBreakdownDTO> SessionPriceBreakdown { get; set; } = new();
        public List<CurrentSessionPriceDTO> CurrentSessionPrices { get; set; } = new();
        public List<CompanyWiseOrderDTO> TotalcompanyWiseOrders { get; set; } = new();
        public List<LatestUsedQrDTO> TotallatestUsedQRs { get; set; } = new();
    }

    public class PlanTypeQtyDTO
    {
        public string PlanType { get; set; } = "";
        public int TotalQty { get; set; }
    }

    public class SessionQtyDTO
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; } = "";
        public int TotalQty { get; set; }
    }

    public class SessionPriceBreakdownDTO
    {
        public string PlanType { get; set; } = "";
        public int SessionId { get; set; }
        public string SessionName { get; set; } = "";
        public int Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CurrentSessionPriceDTO
    {
        public string PlanType { get; set; } = "";
        public int SessionId { get; set; }
        public string SessionName { get; set; } = "";
        public decimal Rate { get; set; }
    }

    public class CompanyWiseOrderDTO
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public int TotalQty { get; set; }
        public int RedeemQty { get; set; }
        public int PendingQty { get; set; }
    }

    public class LatestUsedQrDTO
    {
        public string CompanyName { get; set; } = "";
        public string UniqueCode { get; set; } = "";
        public DateTime? UsedDate { get; set; }
    }
}