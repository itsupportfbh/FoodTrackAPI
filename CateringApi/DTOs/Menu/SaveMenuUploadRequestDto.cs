namespace CateringApi.DTOs.Menu
{
    public class SaveMenuUploadRequestDto
    {
        public int MenuMonth { get; set; }
        public int MenuYear { get; set; }
        public int? CreatedBy { get; set; }
        public List<MenuUploadRowDto> Rows { get; set; } = new();
    }
}
