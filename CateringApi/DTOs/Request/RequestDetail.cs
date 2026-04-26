using CateringApi.DTOs.Item; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace CateringApi.DTOs.Request
{
    public class RequestDetail
    {
        public int Id { get; set; }

        public int RequestHeaderId { get; set; }

        public int? SessionId { get; set; }

        public int CuisineId { get; set; }

        public int? LocationId { get; set; }

        public decimal Qty { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public CateringApi.Models.Session Session { get; set; }
        public string PlanType { get; set; }
    }
}
