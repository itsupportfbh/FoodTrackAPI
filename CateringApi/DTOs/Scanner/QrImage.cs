namespace CateringApi.DTOs.Scanner
{
    public class QrImage
    {

        public int Id { get; set; }                     // Primary Key
        public int Qrcoderequestid { get; set; }             // Foreign Key to Request table
        public byte[] QrCodeImage { get; set; }        // QR code image stored as binary
        public string QrCodeText { get; set; }

        public int? SerialNo { get; set; }

        public string? UniqueCode { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedDate { get; set; }
        // QR code text
        public bool IsActive { get; set; } = true;     // Active flag
        public DateTime CreatedDate { get; set; }      // When record was created
        public string CreatedBy { get; set; }          // Optional creator
        public string UpdatedBy { get; set; }          // Optional updater
        public DateTime? UpdatedDate { get; set; }
    }

    namespace CateringApi.DTOs.Scanner
    {
        public class QrRequestWithImagesDto
        {
            public int Id { get; set; }
            public int RequestId { get; set; }
            public int CompanyId { get; set; }
            public string? CompanyName { get; set; }
            public string? CompanyEmail { get; set; }
            public string? RequestNo { get; set; }
            public int NoOfQR { get; set; }
            public DateTime? QrValidFrom { get; set; }
            public DateTime? QrValidTill { get; set; }

            public List<QrImageItemDto> QrImages { get; set; } = new List<QrImageItemDto>();
        }

        public class QrImageItemDto
        {
            public int Id { get; set; }
            public int QrCodeRequestId { get; set; }
            public string? QrCodeText { get; set; }
            public int? SerialNo { get; set; }
            public string? UniqueCode { get; set; }
            public bool IsUsed { get; set; }
            public DateTime? UsedDate { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }

            public string? QrCodeImageBase64 { get; set; }
        }
    }
}
