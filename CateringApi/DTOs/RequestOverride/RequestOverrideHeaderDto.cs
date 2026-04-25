namespace CateringApi.DTOs.RequestOverride
{
    public class RequestOverrideScreenDto
    {
        public RequestOverrideHeaderDto Header { get; set; } = new();
        public List<RequestOverrideLineDto> Lines { get; set; } = new();
    }

    public class RequestOverrideHeaderDto
    {
        public int RequestHeaderId { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public int CompanyId { get; set; }

        public DateTime RequestFromDate { get; set; }
        public DateTime RequestToDate { get; set; }

        public DateTime OverrideFromDate { get; set; }
        public DateTime OverrideToDate { get; set; }

        public string? Notes { get; set; }
        public string PlanType { get; set; } = string.Empty;

        public int AvailableUsers { get; set; }
    }
    public class RequestOverrideLineDto
    {
        public int RequestOverrideDetailId { get; set; }
        public int RequestDetailId { get; set; }

        public int SessionId { get; set; }
        public string SessionName { get; set; } = "";

        public int CuisineId { get; set; }
        public string CuisineName { get; set; } = "";

        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";

        public int BaseQty { get; set; }
        public int? OverrideQty { get; set; }

        public bool IsCancelled { get; set; }
        public string PlanType { get; set; } = "";

        public int AvailableUsers { get; set; }
    }
    public class RequestOverrideDetail
    {
        public int Id { get; set; }
        public int RequestOverrideId { get; set; }
        public int RequestDetailId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public int LocationId { get; set; }
        public decimal OverrideQty { get; set; }
        public bool IsActive { get; set; }
        public bool IsCancelled { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public RequestOverride RequestOverride { get; set; }
    }
    public class SaveRequestOverrideDto
    {
        public int RequestHeaderId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }

        public List<SaveRequestOverrideLineDto> Lines { get; set; } = new();
    }

    public class SaveRequestOverrideLineDto
    {
        public int RequestDetailId { get; set; }
        public int SessionId { get; set; }
        public int CuisineId { get; set; }
        public int LocationId { get; set; }
        public int BaseQty { get; set; }
        public int OverrideQty { get; set; }
        public bool IsCancelled { get; set; }
        public string PlanType { get; set; } = "";
    }
    public class RequestOverrideListDto
    {
        public int RequestOverrideId { get; set; }
        public int RequestHeaderId { get; set; }
        public string RequestNo { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Notes { get; set; }
        public int LineCount { get; set; }
        public decimal TotalOverrideQty { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    //public class SaveRequestOverrideResultDto
    //{
    //    public int Id { get; set; }
    //    public int TotalQty { get; set; }
    //    public int DifferentQty { get; set; }
    //    public string PlanType { get; set; } = string.Empty;
    //    public string Message { get; set; } = string.Empty;
    //}
    public class SaveRequestOverrideResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string MessageType { get; set; } = "";
        public object? Data { get; set; }
    }
}
