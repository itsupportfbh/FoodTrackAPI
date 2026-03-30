namespace CateringApi.DTOs.Location
{
    public class LocationDTO
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Description { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public int UpdatedBy { get; set; }
        public DateTime UpdatedDate {  get; set; }

        public bool IsActive { get; set; }
    }
}
