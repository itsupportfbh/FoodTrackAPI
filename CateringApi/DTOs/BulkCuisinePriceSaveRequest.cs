
    namespace CateringApi.DTOs
    {
        public class BulkCuisinePriceSaveRequest
        {
            public int CompanyId { get; set; }
            public int SessionId { get; set; }
            public int UpdatedBy { get; set; }
            public List<CuisineRateItem> Rates { get; set; } = new();
        }

        public class CuisineRateItem
        {
            public int CuisineId { get; set; }
            public decimal Rate { get; set; }
            public DateTime EffectiveFrom { get; set; }
        }

        public class CuisineRateViewModel
        {
            public int CuisineId { get; set; }
            public string CuisineName { get; set; } = string.Empty;
            public decimal Rate { get; set; }
            public DateTime? EffectiveFrom { get; set; }
        }

        public class CuisinePriceHistoryDto
        {
            public int Id { get; set; }
            public int PriceId { get; set; }
            public int CompanyId { get; set; }
            public int SessionId { get; set; }
            public int CuisineId { get; set; }
            public string CuisineName { get; set; } = string.Empty;
            public decimal Rate { get; set; }
            public DateTime EffectiveFrom { get; set; }
            public DateTime? EffectiveTo { get; set; }
            public string ActionType { get; set; } = string.Empty;
            public int? CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }

