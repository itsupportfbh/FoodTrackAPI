using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CateringApi.DTOs.RequestOverride
{
    public class RequestOverride
    {


        public int Id { get; set; }

        public int RequestHeaderId { get; set; }
        public int TotalQty { get; set; }

        public int DifferentQty { get; set; }

        public DateTime FromDate { get; set; }
               
        public DateTime ToDate { get; set; }
              
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
