namespace CateringApi.DTOModel
{
    public class QrCodeRequestModel
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;

        public int RequestId { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public int NoofQR { get; set; }
        public decimal TotalQty { get; set; }

        public DateTime QRValidFrom { get; set; }
        public DateTime QRValidTill { get; set; }

        public string PlanType { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }

        public int? OverrideId { get; set; }

        public int ApprovalStatus { get; set; }
        public int? RequestedBy { get; set; }
        public DateTime? RequestedDate { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectionReason { get; set; }

        public string? QRImageBase64 { get; set; }

        public List<QrImageModel> QrImages { get; set; } = new List<QrImageModel>();
    }



    public class QrImageDto
    {
        public int Id { get; set; }
        public int QrCodeRequestId { get; set; }
        public string QrCodeImageBase64 { get; set; } = string.Empty;
        public string QrCodeText { get; set; } = string.Empty;
        public int? SerialNo { get; set; }
        public string UniqueCode { get; set; } = string.Empty;
        public string? PlanType { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDate { get; set; }
    }

    public class RequestDropdownDto
    {
        public int? RequestId { get; set; }
        public int? OverrideId { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime TillDate { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public List<CuisineDropdownDto> Cuisines { get; set; } = new();
    }
    public class QrTargetUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PlanType { get; set; } = "";
        public int CuisineId {  get; set; }
    }

    public class CuisineDropdownDto
    {
        public int CuisineId { get; set; }
        public string CuisineName { get; set; }
        public decimal Qty { get; set; }
    }

    public class QrTargetUserRequestDto
    {
        public int CompanyId { get; set; }
        public string PlanType { get; set; } = "Basic";
        public int Count { get; set; }
        public List<int> CuisineIds { get; set; } = new();
    }
}
