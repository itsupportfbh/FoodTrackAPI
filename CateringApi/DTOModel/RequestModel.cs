namespace CateringApi.DTOModel
{
    public class RequestModel
    {
        public int RequestId { get; set; }

        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
              
             

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal Qty { get; set; }

        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

    }
}
