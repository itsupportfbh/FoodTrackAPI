namespace CateringApi.DTOs.Menu
{
    public class SaveMenuUploadResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
    }
}
