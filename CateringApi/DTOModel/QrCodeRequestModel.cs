namespace CateringApi.DTOModel
{
    public class QrCodeRequestModel
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;
        public int NoofQR { get; set; }
        public DateTime QRValidFrom { get; set; }

        public DateTime QRValidTill { get; set; }
       
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        //
        
        public string? QRImageBase64 { get; set; }

       
       public List<QrImageModel> QrImages { get; set; } = new List<QrImageModel>();




    }

    public class RequestDropdownDto
    {
        public int RequestId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string RequestNo {  get; set; }
        public int Qty { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? TillDate { get; set; }
        public object CompanyEmail { get; internal set; }
    }
}
