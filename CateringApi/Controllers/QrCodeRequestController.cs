using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class QrCodeRequestController : ControllerBase
    {


        private readonly IQrCodeRequestRepository _qrCodeRequestRepository;
        private readonly IQrValidationRepository _qrvalidationrepo;
        public QrCodeRequestController(IQrCodeRequestRepository qrCodeRequestRepository, IQrValidationRepository qrValidationRepository)
        {
            _qrCodeRequestRepository = qrCodeRequestRepository;
            _qrvalidationrepo = qrValidationRepository;
        }


        [HttpGet]
        public Task<List<RequestDropdownDto>> GetRequestIdDropdown()
        {
            return _qrCodeRequestRepository.GetRequestIdDropdown();
        }
        [HttpGet]
        public async Task<IActionResult> DownloadQrZip(int qrcoderequestid)
        {
            var result = await _qrCodeRequestRepository.DownloadQrZip(qrcoderequestid);

            if (result == null)
                return NotFound("No QR images found for download");

            return File(result.Value.ZipBytes, "application/zip", result.Value.FileName);
        }


        [HttpGet]
        public async Task<IActionResult> GetQrDetailsByRequestId(int requestId)
        {
            var data = await _qrCodeRequestRepository.GetQrImageDetailsByRequestId(requestId);
            return Ok(data);
        }
       
        [HttpGet]
        public async Task<List<QrCodeRequestModel>> GetAllQRList()
        {
            return await _qrCodeRequestRepository.GetAllQRList();
        }
        
        [HttpGet]
        public Task<List<QrCodeRequestModel>> GetAllQRModelbyrequestId(int id, int requestId)
        {
            return _qrCodeRequestRepository.GetAllQRModelbyId(id, requestId);
        }

        [HttpGet]
        public Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id)
        {
            return _qrCodeRequestRepository.GetAllQRModelbyId(id);
        }
        
        

        [HttpPost]
        public async Task<IActionResult> SendQrEmail([FromBody] SendEmailDto model)
        {
            var result = await _qrCodeRequestRepository.SendQrEmailAsync(model);
            return Ok(new
            {
                success = result,
                message = "QR email sent successfully"
            });

        }
        [HttpPost]

        public async Task<IActionResult> AddUpdateQrWithImagesAsync([FromBody] QrCodeRequestModel model)
        {
            var result = await _qrCodeRequestRepository.AddUpdateQrWithImagesAsync(model);
            return Ok(result);
        }

        [HttpPost]
        public async Task<List<QrResultDto>> GenerateUniqueQrs(QrCodeRequest model)
        {
            return await _qrCodeRequestRepository.GenerateUniqueQrs(model);
        }
        [HttpGet]
        public async Task<QrValidationResult> ValidateScanAsync(string UniqueCode, int RequestId, int CompanyId)
        {
            return  await _qrvalidationrepo.ValidateScanAsync(UniqueCode, RequestId, CompanyId); 
        }
        [HttpDelete("DeleteQR/{id}")]
        public async Task<IActionResult> DeleteQR(int id, [FromQuery] int userId)
        {
            var data = await _qrCodeRequestRepository.DeleteQrCode(id, userId);

            if (data == null)
            {
                return NotFound(new { message = "QR record not found" });
            }

            return Ok(new
            {
                message = "QR record deleted successfully",
                data
            });


            //[HttpGet]
            //public async Task<List<QrCodeRequest>> GetAllQR()
            //{
            //    return await _qrCodeRequestRepository.GetAllQR();
            //}
            //[HttpGet]
            //public Task<List<QrCodeRequest>> GetAllQRModel()
            //{
            //    return _qrCodeRequestRepository.GetAllQRModel();
            //}
            //[HttpPost]
            //public async Task<QrResultDto> GenerateQr(QrCodeRequest model)
            //{
            //    return await _qrCodeRequestRepository.GenerateQr(model);
            //}
        }






    }
}
