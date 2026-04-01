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
        public Task<List<QrCodeRequestModel>> GetAllQRModelbyrequestId(int id, string requestId)
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
        public async Task<IActionResult> SendQrEmail([FromBody] SendQrEmailDto model)
        {
            var result = await _qrCodeRequestRepository.SendQrEmailAsync(model);

            if (result)
                return Ok("Email sent successfully");

            return BadRequest("Failed to send email");
        }
        [HttpPost]
        public Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model)
        {
            return _qrCodeRequestRepository.AddUpdateQrWithImagesAsync<QrCodeRequestModel>(model);

        }

    }
}
