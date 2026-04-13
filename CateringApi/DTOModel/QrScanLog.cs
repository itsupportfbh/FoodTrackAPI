using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CateringApi.DTOModel
{
    [Table("QrScanLog")]
    public class QrScanLog
    {
        [Key]
        public int Id { get; set; }

        public int QrImageId { get; set; }
        public int QrCodeRequestId { get; set; }
        public int RequestId { get; set; }
        public int SessionId { get; set; }

        public DateTime ScanDate { get; set; }
        public DateTime ScanDateTime { get; set; }

        public string UniqueCode { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
        public string? Message { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
    }

  
}
