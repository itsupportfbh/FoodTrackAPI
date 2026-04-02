namespace CateringApi.DTOModel
{
    public class QrImageModel
    {

        public int Id { get; set; }   
        // Primary Key
        public int Qrcoderequestid { get; set; }             // Foreign Key to Request table
        public string QrCodeImageBase64 { get; set; }      // QR code image stored as binary
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
}
