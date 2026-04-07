using CateringApi.DTOs.Scanner;
using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface IQrValidationRepository
    {
        Task<QrValidationResult> ValidateScanAsync(string UniqueCode, int RequestId, int CompanyId);
        QrImage? GetQrImageByUniqueCode(string uniqueCode);
        Task<RequestHeader?> GetQrRequestByIdAsync(int requestId);
        Task MarkQrAsUsedAsync(int qrImageId, DateTime usedDate);
        Task DeactivateRequestAndImagesAsync(int requestId, string UniqueCode);


    }
}
