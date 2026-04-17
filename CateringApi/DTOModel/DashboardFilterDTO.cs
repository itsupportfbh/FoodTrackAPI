namespace CateringApi.DTOModel
{
    public class DashboardFilterDTO
    {
        public List<int>? CompanyIds { get; set; }
        public List<int>? SessionIds { get; set; }
        public List<int>? LocationIds { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
