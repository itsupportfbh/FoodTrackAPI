namespace CateringApi.DTOs.Menu
{
    public class MenuUploadRowDto
    {
        public string Date { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string CuisineName { get; set; } = string.Empty;
        public string SetName { get; set; } = string.Empty;
        public string? Item1 { get; set; }
        public string? Item2 { get; set; }
        public string? Item3 { get; set; }
        public string? Item4 { get; set; }
        public string? Notes { get; set; }
    }
}
