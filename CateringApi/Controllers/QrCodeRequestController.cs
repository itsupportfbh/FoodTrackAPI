using CateringApi.DTOModel;
using CateringApi.DTOs.Common;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QrCodeRequestController : ControllerBase
    {
        private readonly IQrCodeRequestRepository _qrCodeRequestRepository;
        private readonly IQrValidationRepository _qrvalidationrepo;

        public QrCodeRequestController(
            IQrCodeRequestRepository qrCodeRequestRepository,
            IQrValidationRepository qrValidationRepository)
        {
            _qrCodeRequestRepository = qrCodeRequestRepository;
            _qrvalidationrepo = qrValidationRepository;
        }

        [HttpGet("GetRequestIdDropdown")]
        public async Task<IActionResult> GetQrPendingDropdown()
        {
            var companyIdClaim = User.Claims.FirstOrDefault(x => x.Type == "CompanyId")?.Value;

            var loginCompanyId = string.IsNullOrWhiteSpace(companyIdClaim)
                ? 0
                : Convert.ToInt32(companyIdClaim);

            var data = await _qrCodeRequestRepository.GetQrPendingDropdown(loginCompanyId);

            return Ok(data);
        }

        [HttpGet("DownloadQrZip")]
        public async Task<IActionResult> DownloadQrZip(int qrcoderequestid)
        {
            var result = await _qrCodeRequestRepository.DownloadQrZip(qrcoderequestid);

            if (result == null)
                return NotFound("No QR images found for download");

            return File(result.Value.ZipBytes, "application/zip", result.Value.FileName);
        }

        [HttpGet("GetQrDetailsByRequestId")]
        public async Task<IActionResult> GetQrDetailsByRequestId(int requestId)
        {
            var data = await _qrCodeRequestRepository.GetQrImageDetailsByRequestId(requestId);
            return Ok(data);
        }

        [HttpGet("GetAllQRList")]
        public async Task<IActionResult> GetAllQRList()
        {
            int roleId = Convert.ToInt32(User.Claims.FirstOrDefault(x => x.Type == "RoleId")?.Value ?? "0");
            int companyId = Convert.ToInt32(User.Claims.FirstOrDefault(x => x.Type == "CompanyId")?.Value ?? "0");

            var data = await _qrCodeRequestRepository.GetAllQRList(roleId, companyId);
            return Ok(data);
        }

        [HttpPost("SendQrEmail")]
        public async Task<IActionResult> SendQrEmail([FromBody] SendEmailDto model)
        {
            var result = await _qrCodeRequestRepository.SendQrEmailAsync(model);
            return Ok(new
            {
                success = result,
                message = "QR email sent successfully"
            });
        }

        [HttpPost("AddUpdateQrWithImages")]
        public async Task<IActionResult> AddUpdateQrWithImagesAsync([FromBody] QrCodeRequestModel model)
        {
            var result = await _qrCodeRequestRepository.AddUpdateQrWithImagesAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("GenerateUniqueQrs")]
        public async Task<IActionResult> GenerateUniqueQrs([FromBody] QrCodeRequest model)
        {
            var result = await _qrCodeRequestRepository.GenerateUniqueQrs(model);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("ValidateScan")]
        public async Task<QrValidationResult> ValidateScanAsync(string UniqueCode)
        {
            return await _qrvalidationrepo.ValidateScanAsync(UniqueCode);
        }

        [HttpDelete("DeleteQR/{id}")]
        public async Task<IActionResult> DeleteQR(int id, [FromQuery] int userId)
        {
            var data = await _qrCodeRequestRepository.DeleteQrCode(id, userId);

            if (data == null)
            {
                return NotFound(new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "QR record not found",
                    MessageType = "error"
                });
            }

            return Ok(new ApiResponseDto
            {
                IsSuccess = true,
                Message = "QR record deleted successfully",
                MessageType = "success",
                Data = data
            });
        }

        [HttpPost("submit-qr-approval")]
        public async Task<IActionResult> SubmitQrApproval([FromBody] QrCodeRequestModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid request data.",
                        MessageType = "error"
                    });
                }

                var result = await _qrCodeRequestRepository.SubmitQrApprovalRequestAsync(model);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    MessageType = "error"
                });
            }
        }

        [HttpPost("approve-qr-request/{id}")]
        public async Task<IActionResult> ApproveQrRequest(int id, [FromBody] int approvedBy)
        {
            var result = await _qrCodeRequestRepository.ApproveQrRequestAsync(id, approvedBy);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("reject-qr-request/{id}")]
        public async Task<IActionResult> RejectQrRequest(int id, [FromBody] RejectQrDto model)
        {
            var result = await _qrCodeRequestRepository.RejectQrRequestAsync(id, model.RejectedBy, model.Reason);

            return Ok(new ApiResponseDto
            {
                IsSuccess = true,
                Message = result,
                MessageType = "success"
            });
        }
        [HttpGet("GetQrTargetUsers")]
        public async Task<IActionResult> GetQrTargetUsers(
            int companyId,
            string planType,
            int count,
            [FromQuery] List<int> cuisineIds)
        {
            var data = await _qrCodeRequestRepository.GetQrTargetUsersAsync(
                companyId,
                planType,
                count,
                cuisineIds
            );

            return Ok(data);
        }


        [HttpGet("GetLockedPlanTypes")]
        public async Task<IActionResult> GetLockedPlanTypes(int requestId)
        {
            var data = await _qrCodeRequestRepository.GetLockedPlanTypesAsync(requestId);

            return Ok(new
            {
                isSuccess = true,
                data = data
            });
        }
        [HttpPost("BackupLastMonthQrData")]
        public async Task<IActionResult> BackupLastMonthQrData()
        {
            var result = await _qrCodeRequestRepository.BackupLastMonthQrDataAsync();
            return Ok(new
            {
                messageType = "success",
                message = result
            });
        }


    }
}