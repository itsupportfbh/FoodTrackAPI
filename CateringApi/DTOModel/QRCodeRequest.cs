using System.ComponentModel.DataAnnotations;

namespace CateringApi.DTOModel
{
    public class QRCodeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; }

        [StringLength(150)]
        public string CompanyEmail { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestId { get; set; }

        [Required]
        public int NoOfQR { get; set; }

        [Required]
        public DateTime QRValidFrom { get; set; }

        [Required]
        public DateTime QRValidTill { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }
    }
}
