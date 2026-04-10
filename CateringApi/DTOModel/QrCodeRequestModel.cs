namespace CateringApi.DTOModel
{
    public class QrCodeRequestModel
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;

        public int RequestId { get; set; }
        public string RequestNo { get; set; }
        public int NoofQR { get; set; }
        public DateTime QRValidFrom { get; set; }

        public DateTime QRValidTill { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public int OverrideId { get; set; }

        

        public string? QRImageBase64 { get; set; }


        public List<QrImageModel> QrImages { get; set; } = new List<QrImageModel>();




    }

    

    public class QrImageDto
    {
        public int Id { get; set; }
        public int QrCodeRequestId { get; set; }
        public string QrCodeImageBase64 { get; set; }
        public string QrCodeText { get; set; }
        public int? SerialNo { get; set; }
        public string UniqueCode { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class RequestDropdownDto
    {
        public int RequestId { get; set; }
        public int? OverrideId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string RequestNo {  get; set; }
        public int Qty { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? TillDate { get; set; }
        public string CompanyEmail { get; set; } = string.Empty;
        public int? TotalQty { get; set; }
        public int DifferentQty { get; set; }
        public string SourceType { get; set; }   // "REQUEST" or "OVERRIDE"
        public string DisplayText { get; set; }  // dropdown display
    }
}
