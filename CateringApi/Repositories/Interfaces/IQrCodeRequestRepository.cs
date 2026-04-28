using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IQrCodeRequestRepository
    {
        //  public Task<List<RequestDropdownDto>> GetRequestIdDropdown();
        // public Task<List<QrCodeRequest>> GetAllQR();
        Task<List<RequestDropdownDto>> GetQrPendingDropdown(int loginCompanyId);
        Task<List<QrCodeRequestModel>> GetAllQRList(int loginRoleId, int loginCompanyId);
        public  Task<QrRequestWithImagesDto?> GetQrImageDetailsByRequestId(int requestId);
           //    public Task<List<QrCodeRequest>> GetAllQRModel();
        public  Task<QrCodeRequestModel> DeleteQrCode(int id, int userId);
        // public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id, int requestId);
        //  public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id);

        Task<ApiResponseDto> AddUpdateQrWithImagesAsync(QrCodeRequestModel model);
        // public  Task<QrResultDto> GenerateQr(QrCodeRequest model);
        Task<ApiResponse> GenerateUniqueQrs(QrCodeRequest model);
        Task<(byte[] ZipBytes, string FileName)?> DownloadQrZip(int qrcoderequestid);
        Task<bool> SendQrEmailAsync(SendEmailDto model);
        // Task<T> AddUpdateQrWithImagesAsync<T>(T model);
         Task<ApiResponseDto> SubmitQrApprovalRequestAsync(QrCodeRequestModel model);
        Task<ApiResponseDto> ApproveQrRequestAsync(int qrCodeRequestId, int approvedBy);
        Task<string> RejectQrRequestAsync(int qrCodeRequestId, int rejectedBy, string reason);
        Task<QrUserCountValidationDto> ValidateCompanyUserCountAsync(
     int requestId,
     int? overrideId,
     string? planType
 );
    
     Task<List<QrTargetUserDto>> GetQrTargetUsersAsync(
     int companyId,
     string planType,
     int count,
     List<int> cuisineIds);


        Task<List<LockedPlanTypeDto>> GetLockedPlanTypesAsync(int requestId);
        Task<string> BackupLastMonthQrDataAsync();
    }
}
