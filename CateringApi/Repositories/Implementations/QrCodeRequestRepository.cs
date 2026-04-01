using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace CateringApi.Repositories.Implementations
{
    public class QrCodeRequestRepository : IQrCodeRequestRepository
    {


        private readonly FoodDBContext _context;

        public QrCodeRequestRepository(FoodDBContext context)
        {
            _context = context;
        }

        public async Task<List<QrCodeRequest>> GetAllQR()
        {
            return await _context.QrCodeRequest.ToListAsync();
        }

        //public async Task<List<RequestModel>> GetAllDetallstogenerateQR( int id)
        //{
        //    var result=_context.Request.Where(x=>x.RequestId==id).
        //}


        public async Task<List<RequestDropdownDto>> GetRequestIdDropdown()
        {
            var result = await (from r in _context.Requests
                                join c in _context.company
                                on r.CompanyId equals c.Id
                                where r.IsActive
                                select new RequestDropdownDto
                                {

                                    RequestId = Convert.ToInt32(r.Id),
                                    RequestNo = r.RequestNo,

                                    CompanyId = r.CompanyId,

                                    // ✅ From Company table
                                    CompanyName = c.CompanyName,
                                    CompanyEmail = c.Email,

                                    // ✅ From Request table
                                    Qty = Convert.ToInt32(r.TotalQty),
                                    FromDate = r.FromDate,
                                    TillDate = r.ToDate
                                })
                          .ToListAsync();

            return result;
        }
        public async Task<List<QrCodeRequestModel>> GetAllQRModel()
        {
            var data = await _context.QrCodeRequest
                .Select(a => new QrCodeRequestModel
                {
                    Id = a.Id,
                    CompanyId = a.CompanyId,
                    CompanyName = a.CompanyName,
                    CompanyEmail = a.CompanyEmail,
                    RequestId = a.RequestId,
                    NoofQR = a.NoofQR,

                    QRValidFrom = a.QRValidFrom,
                    QRValidTill = a.QRValidTill,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    CreatedBy = a.CreatedBy,
                    UpdatedBy = a.UpdatedBy
                })
                .ToListAsync();

            // ✅ Convert image to Base64 AFTER fetching from DB
           

            return data;
        }

        public async Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id)
        {
            return await _context.QrCodeRequest
                .Where(x => x.Id == id)
                .Select(a => new QrCodeRequestModel
                {
                    Id = a.Id,
                    CompanyId = a.CompanyId,
                    CompanyName = a.CompanyName,
                    CompanyEmail = a.CompanyEmail,
                    RequestId = a.RequestId,
                    NoofQR = a.NoofQR,
                    //QRImage = a.QRImage,
                    //QRText = a.QRText,

                    // ✅ Convert to Base64 for frontend
                    //QRImageBase64 = a.QRImage != null
                    //    ? Convert.ToBase64String(a.QRImage)
                    //    : null,

                    QRValidFrom = a.QRValidFrom,
                    QRValidTill = a.QRValidTill,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    CreatedBy = a.CreatedBy,
                    UpdatedBy = a.UpdatedBy
                })
                .ToListAsync();
        }

        public async Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id, string requestId)
        {
            var data = await _context.QrCodeRequest
                .Where(x => x.Id == id && x.RequestId == requestId)
                .Select(a => new QrCodeRequestModel
                {
                    Id = a.Id,
                    CompanyId = a.CompanyId,
                    CompanyName = a.CompanyName,
                    CompanyEmail = a.CompanyEmail,
                    RequestId = a.RequestId,
                    NoofQR = a.NoofQR,
                    //QRImage = a.QRImage,
                    //QRText = a.QRText,
                    QRValidFrom = a.QRValidFrom,
                    QRValidTill = a.QRValidTill,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    CreatedBy = a.CreatedBy,
                    UpdatedBy = a.UpdatedBy
                })
                .ToListAsync();

            // ✅ Convert after fetching (safe)
            

            return data;
        }
       
        public QrResultDto GenerateQr(QrCodeRequest model)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.RequestId) ||
                string.IsNullOrWhiteSpace(model.CompanyName))
            {
                return null; // ❗ don't throw exception
            }

            var qrDataObject = new
            {
                model.RequestId,
                model.CompanyId,
                model.CompanyName
            };

            string qrText = JsonSerializer.Serialize(qrDataObject);

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);

            byte[] qrBytes = qrCode.GetGraphic(20);

            return new QrResultDto
            {
                Text = qrText,
                ImageBytes = qrBytes,
                ImageBase64 = Convert.ToBase64String(qrBytes)
            };
        }

        public async Task<bool> SendQrEmailAsync(SendQrEmailDto model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Email))
                    throw new Exception("Email is required");

                // 🔹 Create Mail
                var message = new MailMessage
                {
                    From = new MailAddress("vimalakarolin@gmail.com"), // change
                    Subject = "Your QR Code",
                    Body = $"<h3>Your QR Code</h3><p>{model.QrText}</p>",
                    IsBodyHtml = true
                };

                message.To.Add(model.Email);

                // 🔹 Attach QR Image
                if (!string.IsNullOrEmpty(model.QrImageBase64))
                {
                    byte[] bytes = Convert.FromBase64String(model.QrImageBase64);
                    var stream = new MemoryStream(bytes);

                    var attachment = new Attachment(stream, "QRCode.png", "image/png");
                    message.Attachments.Add(attachment);
                }

                // 🔹 SMTP Config (Move to appsettings later)
                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(
        "noreply@cspl.sg",
        "Dad68527" // or App Password if MFA enabled
    ),
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network

                };

                await smtp.SendMailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                // 🔹 Log error if you have logger
                throw new Exception("Email sending failed: " + ex.Message);
            }
        }

        public Task<QrCodeRequestModel> AddUpdateQR(QrCodeRequestModel qrModel)
        {
            throw new NotImplementedException();
        }






        /// Add or update QR request and save all generated QR images
        /// </summary>
        public async Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                QrCodeRequest qrRequest;

                // -------------------------------
                // Insert or update main QR request
                // -------------------------------
                if (model.Id > 0)
                {
                    qrRequest = await _context.QrCodeRequest
                        .FirstOrDefaultAsync(x => x.Id == model.Id)
                        ?? throw new Exception("QR request not found");

                    // Update existing fields
                    qrRequest.CompanyId = model.CompanyId;
                    qrRequest.CompanyName = model.CompanyName;
                    qrRequest.CompanyEmail = model.CompanyEmail;
                    qrRequest.NoofQR = model.NoofQR;
                    qrRequest.QRValidFrom = model.QRValidFrom;
                    qrRequest.QRValidTill = model.QRValidTill;
                    qrRequest.IsActive = model.IsActive;
                    qrRequest.UpdatedDate = DateTime.UtcNow;
                    qrRequest.UpdatedBy = model.UpdatedBy;

                    _context.QrCodeRequest.Update(qrRequest);
                }
                else
                {
                    // New record
                    qrRequest = new QrCodeRequest
                    {
                        RequestId = model.RequestId,
                        CompanyId = model.CompanyId,
                        CompanyName = model.CompanyName,
                        CompanyEmail = model.CompanyEmail,
                        NoofQR = model.NoofQR,
                        QRValidFrom = model.QRValidFrom,
                        QRValidTill = model.QRValidTill,
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy,
                    };

                    _context.QrCodeRequest.Add(qrRequest);
                }

                await _context.SaveChangesAsync(); // Save main request

                // -------------------------------
                // Remove old QR images if updating
                // -------------------------------
                if (model.Id > 0)
                {
                    var existingQrs = _context.QrImage.Where(x => x.Qrcoderequestid == qrRequest.Id);
                    _context.QrImage.RemoveRange(existingQrs);
                }

                // -------------------------------
                // Insert new QR images
                // -------------------------------
                if (model.QrImages != null && model.QrImages.Any())
                {
                    foreach (var qr in model.QrImages)
                    {
                        var qrImage = new QrImage
                        {
                            Qrcoderequestid = qrRequest.Id,  // ✅ FK always set
                            QrCodeText = qr.QrCodeText,
                            QrCodeImage = Convert.FromBase64String(qr.QrCodeImageBase64!),
                            IsActive = qr.IsActive,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = model.CreatedBy.ToString()
                        };
                        _context.QrImage.Add(qrImage);
                    }
                }

                await _context.SaveChangesAsync(); // Save all QR images
                await transaction.CommitAsync();

                // -------------------------------
                // Map back to DTO
                // -------------------------------
                model.Id = qrRequest.Id;
                return model;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task<T> AddUpdateQrWithImagesAsync<T>(T model)
        {
            throw new NotImplementedException();
        }
    }











}




    

