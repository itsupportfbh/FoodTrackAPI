using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IQrCodeRequestRepository
    {
        public Task<List<RequestDropdownDto>> GetRequestIdDropdown();
               public Task<List<QrCodeRequestModel>> GetAllQRModel();
        public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id, int requestId);
        public  Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id);

        public Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model);
        public QrResultDto GenerateQr(QrCodeRequest model);
        public List<QrResultDto> GenerateUniqueQrs(QrCodeRequest model);
        Task<bool> SendQrEmailAsync(SendEmailDto model);
       // Task<T> AddUpdateQrWithImagesAsync<T>(T model);
    }
}
