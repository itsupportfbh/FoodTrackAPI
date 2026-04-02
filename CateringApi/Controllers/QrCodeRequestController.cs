using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
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
        public QrCodeRequestController(IQrCodeRequestRepository qrCodeRequestRepository)
        {
            _qrCodeRequestRepository = qrCodeRequestRepository;
        }


        [HttpGet]
        public Task<List<RequestDropdownDto>> GetRequestIdDropdown()
        {
            return _qrCodeRequestRepository.GetRequestIdDropdown();
        }



        [HttpGet]
        public Task<List<QrCodeRequestModel>> GetAllQRModel()
        {
            return _qrCodeRequestRepository.GetAllQRModel();
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
        public QrResultDto GenerateQr(QrCodeRequest model)
        {
            return _qrCodeRequestRepository.GenerateQr(model);
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
        public List<QrResultDto> GenerateUniqueQrs(QrCodeRequest model)
        {
            return _qrCodeRequestRepository.GenerateUniqueQrs(model);
        }

    }
}
