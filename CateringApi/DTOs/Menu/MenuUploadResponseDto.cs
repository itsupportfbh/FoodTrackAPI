namespace CateringApi.DTOs.Menu
{
    public class MenuUploadResponseDto
    {
        public int Id { get; set; }
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
    public class MenuPdfDto
    {
        public string Date { get; set; }
        public string SessionName { get; set; }
        public string CuisineName { get; set; }
        public string SetName { get; set; }
        public string Item1 { get; set; }
        public string Item2 { get; set; }
        public string Item3 { get; set; }
        public string Item4 { get; set; }
        public string Notes { get; set; }
    }
}
