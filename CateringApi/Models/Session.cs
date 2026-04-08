using System.ComponentModel.DataAnnotations;

namespace CateringApi.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string SessionName { get; set; }
        public string Description { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
