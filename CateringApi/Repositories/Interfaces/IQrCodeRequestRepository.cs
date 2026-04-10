using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IQrCodeRequestRepository
    {
        public Task<List<RequestDropdownDto>> GetRequestIdDropdown();
       // public Task<List<QrCodeRequest>> GetAllQR();

        public  Task<List<QrCodeRequestModel>> GetAllQRList();
        public  Task<QrRequestWithImagesDto?> GetQrImageDetailsByRequestId(int requestId);
           //    public Task<List<QrCodeRequest>> GetAllQRModel();
        public  Task<QrCodeRequestModel> DeleteQrCode(int id, int userId);
       // public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id, int requestId);
      //  public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id);

        public Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model);
       // public  Task<QrResultDto> GenerateQr(QrCodeRequest model);
        public Task<List<QrResultDto>> GenerateUniqueQrs(QrCodeRequest model);
        Task<(byte[] ZipBytes, string FileName)?> DownloadQrZip(int qrcoderequestid);
        Task<bool> SendQrEmailAsync(SendEmailDto model);
       // Task<T> AddUpdateQrWithImagesAsync<T>(T model);
    }
}
