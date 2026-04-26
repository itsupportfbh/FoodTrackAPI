namespace CateringApi.DTOs.MealPlan
{
    public class ShowQrDTO
    {
        public int QrImageId { get; set; }
        public byte[] QrCodeImage { get; set; }
        public string QrCodeText { get; set; }
        public string PlanType { get; set; }

    }
}
