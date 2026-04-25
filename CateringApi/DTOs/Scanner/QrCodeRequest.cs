using System.ComponentModel.DataAnnotations;

namespace CateringApi.DTOs.Scanner
{
    public class QrCodeRequest
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string CompanyName { get; set; }

        public string CompanyEmail { get; set; }

        public int RequestId { get; set; }

        public int NoofQR { get; set; }

        public DateTime QRValidFrom { get; set; }

        public DateTime QRValidTill { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int CreatedBy { get; set; }

        public int UpdatedBy { get; set; }

        public int? OverrideId { get; set; }

        // Approval Flow
        public int ApprovalStatus { get; set; }   // 0 = Pending, 1 = Approved, 2 = Rejected

        public int? RequestedBy { get; set; }

        public DateTime? RequestedDate { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public int? RejectedBy { get; set; }

        public DateTime? RejectedDate { get; set; }

        public string? RejectionReason { get; set; }
        public string PlanType { get; set; }
    }
    public class RejectQrDto
    {
        public int RejectedBy { get; set; }
        public string Reason { get; set; }
    }
    public class QrResultDto
    {
        public string Text { get; set; }
        public byte[] ImageBytes { get; set; }
        public string ImageBase64 { get; set; }
        public int? SerialNo { get; set; }
        public string? UniqueCode { get; set; }
        public bool IsUsed { get; set; } = false;

        public DateTime? UsedDate { get; set; }





    }

    public class SendQrEmailDto
    {
        public string Email { get; set; }
        public string QrText { get; set; }
        public string QrImageBase64 { get; set; }
        public int? SerialNo { get; set; }

        public string? UniqueCode { get; set; }

       

    }
    public class DateSegment
    {
        public DateTime FromDate { get; set; }
        public DateTime TillDate { get; set; }
    }
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
    public class ApiResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string MessageType { get; set; } = "success"; // success, warning, error
    }
}
