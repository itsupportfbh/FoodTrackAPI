using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private readonly SmtpSettings _smtpSettings;


        public QrCodeRequestRepository(FoodDBContext context, IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _smtpSettings = smtpOptions.Value;
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
            var list = await _context.RequestHeader.Where(x=>x.IsActive==true).ToListAsync();




            var result = await (from r in _context.RequestHeader
                                join c in _context.CompanyMaster
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

        public async Task<List<QrCodeRequestModel>> GetAllQRModelbyId(int id, Int32 requestId)
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
        //final
        public async Task<QrResultDto> GenerateQr(QrCodeRequest model)
        {
            if (model == null ||
                model.RequestId <= 0 ||
                string.IsNullOrWhiteSpace(model.CompanyName))
            {
                return null;
            }

            var requestHeader = await _context.RequestHeader
                .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.IsActive == true);

            if (requestHeader == null)
            {
                return null;
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

            // set IsActive = false after QR generated
            requestHeader.IsActive = false;
            requestHeader.UpdatedDate = DateTime.UtcNow; // if you have this column
            requestHeader.UpdatedBy = model.UpdatedBy;   // if you have this column

            await _context.SaveChangesAsync();

            return new QrResultDto
            {
                Text = qrText,
                ImageBytes = qrBytes,
                ImageBase64 = Convert.ToBase64String(qrBytes)
            };
        }


        public async Task<List<QrResultDto>> GenerateUniqueQrs(QrCodeRequest model)
        {
            var result = new List<QrResultDto>();

            if (model == null ||
                model.RequestId <= 0 ||
                model.CompanyId <= 0 ||
                string.IsNullOrWhiteSpace(model.CompanyName) ||
                model.NoofQR <= 0)
            {
                return result;
            }

            // Check RequestHeader is active
            var requestHeader = await _context.RequestHeader
                .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.IsActive == true);

            if (requestHeader == null)
            {
                return result;
            }

            int totalQty = Convert.ToInt32(model.NoofQR);

            using var qrGenerator = new QRCodeGenerator();

            for (int i = 1; i <= totalQty; i++)
            {
                string uniqueCode = $"CSPL-{model.RequestId}-CMP-{model.CompanyName}-SR-{i:0000}";

                var qrDataObject = new
                {
                    model.RequestId,
                    model.CompanyId,
                    model.CompanyName,
                    UniqueCode = uniqueCode,
                    ValidFrom = model.QRValidFrom,
                    ValidTill = model.QRValidTill,
                    SerialNo = i,
                    UsedDate = (DateTime?)null,
                    IsUsed = false
                };

                string qrText = JsonSerializer.Serialize(qrDataObject);

                using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);

                byte[] qrBytes = qrCode.GetGraphic(20);

                result.Add(new QrResultDto
                {
                    Text = qrText,
                    ImageBytes = qrBytes,
                    ImageBase64 = Convert.ToBase64String(qrBytes),
                    UniqueCode = uniqueCode,
                    SerialNo = i,
                    UsedDate = null,
                    IsUsed = false
                });
            }

            // After successful generation, update RequestHeader
            requestHeader.IsActive = false;

            // optional fields if available in your table
            // requestHeader.UpdatedBy = model.UpdatedBy;
            // requestHeader.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<bool> SendQrEmailAsync(SendEmailDto model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Email))
                    throw new Exception("Email is required");

                if (model.QrItems == null || !model.QrItems.Any())
                    throw new Exception("QR items are required");

                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.From),
                    Subject = "Your QR Codes",
                    IsBodyHtml = true
                };

                message.To.Add(model.Email);

                var bodyBuilder = new System.Text.StringBuilder();

                bodyBuilder.Append(@"
<html>
<head>
  <meta charset='UTF-8'>
  <title > Your QR Codes </title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif; color:#333333;'>
  <div style='width:100%; background-color:#f4f6f8; padding:30px 0;'>
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width:900px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 14px rgba(0,0,0,0.08);'>
      
      <tr>
        <td style='background: linear-gradient(135deg, #b7410e, #e25822); padding:28px 32px; text-align:center;'>
          <h1 style='margin:0; font-size:28px; color:#ffffff; font-weight:700; letter-spacing:0.3px;'>
            CSPL
          </h1>
          <p style='margin:8px 0 0; font-size:14px; color:#e8f0ff;'>
            Please find all generated QR codes attached below.
          </p>
        </td>
      </tr>

      <tr style=''>
        <td style='padding:28px 32px 12px;background: linear-gradient(135deg, #f4e1c1, #e6c79c);' >
          <p style='margin:0 0 16px; font-size:15px; line-height:1.6; color:#4b5563;'>
            Hello,
          </p>
          <p style='margin:0; font-size:15px; line-height:1.6; color:#4b5563;'>
            The QR codes for your request have been generated successfully. A summary is shown below, and the QR image files are attached with this email.
          </p>
        </td>
      </tr>

      <tr>
        <td style='padding:12px 32px 30px;background: linear-gradient(135deg, #f4e1c1, #e6c79c);'>
          <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='border-collapse:collapse; width:100%; border:1px solid #e5e7eb; border-radius:10px; overflow:hidden;'>
            <tr style='background-color:#f9fafb;'>
              <th style='padding:14px 12px; border:1px solid #e5e7eb; font-size:14px; font-weight:700; color:#111827; text-align:center;'>S.No</th>
              <th style='padding:14px 12px; border:1px solid #e5e7eb; font-size:14px; font-weight:700; color:#111827; text-align:left;'>Unique Code</th>
              <th style='padding:14px 12px; border:1px solid #e5e7eb; font-size:14px; font-weight:700; color:#111827; text-align:center;'>Status</th>
              <th style='padding:14px 12px; border:1px solid #e5e7eb; font-size:14px; font-weight:700; color:#111827; text-align:center;'>Used Date</th>
            </tr>
");

                int count = 1;

                foreach (var qr in model.QrItems)
                {
                    string statusHtml = qr.IsUsed
                        ? "<span style='display:inline-block; padding:6px 12px; background-color:#dcfce7; color:#166534; border-radius:999px; font-size:12px; font-weight:700;'>Used</span>"
                        : "<span style='display:inline-block; padding:6px 12px; background-color:#fee2e2; color:#991b1b; border-radius:999px; font-size:12px; font-weight:700;'>Not Used</span>";

                    string usedDateText = qr.UsedDate.HasValue
                        ? qr.UsedDate.Value.ToString("dd-MMM-yyyy hh:mm tt")
                        : "-";

                    string rowBg = count % 2 == 0 ? "#fcfcfd" : "#ffffff";

                    bodyBuilder.Append($@"
            <tr style='background-color:{rowBg};'>
              <td style='padding:12px; border:1px solid #e5e7eb; font-size:14px; color:#374151; text-align:center;'>{count}</td>
              <td style='padding:12px; border:1px solid #e5e7eb; font-size:14px; color:#111827; font-weight:600; text-align:left;'>{qr.UniqueCode}</td>
              <td style='padding:12px; border:1px solid #e5e7eb; text-align:center;'>{statusHtml}</td>
              <td style='padding:12px; border:1px solid #e5e7eb; font-size:13px; color:#4b5563; text-align:center;'>{usedDateText}</td>
            </tr>");

                    if (!string.IsNullOrWhiteSpace(qr.QrImageBase64))
                    {
                        byte[] bytes = Convert.FromBase64String(qr.QrImageBase64);
                        var stream = new MemoryStream(bytes);

                        string safeName = string.IsNullOrWhiteSpace(qr.UniqueCode)
                            ? $"QRCode_{count}"
                            : qr.UniqueCode.Replace(" ", "_").Replace("/", "_");

                        var attachment = new Attachment(stream, $"{safeName}.png", "image/png");
                        message.Attachments.Add(attachment);
                    }

                    count++;
                }

                bodyBuilder.Append(@"
          </table>
        </td>
      </tr>

      <tr>
        <td style='padding:0 32px 24px;'>
          <div style='background:#f9fafb; border:1px solid #e5e7eb; border-radius:10px; padding:16px 18px;'>
            <p style='margin:0; font-size:13px; line-height:1.7; color:#6b7280;'>
              <strong style='color:#111827;'>Note:</strong> Please keep these QR codes safe and do not share them with unauthorized users.
              Each QR code is attached as a separate PNG file for easy download and use.
            </p>
          </div>
        </td>
      </tr>

      <tr>
        <td style='padding:18px 32px; background-color:#f3f4f6; text-align:center;'>
          <p style='margin:0; font-size:12px; color:#6b7280;'>
            This is an automated email from the QR Code Management System.
          </p>
        </td>
      </tr>

    </table>
  </div>
</body>
</html>
");

                message.Body = bodyBuilder.ToString();

                using var smtp = new SmtpClient(_smtpSettings.SmtpHost, _smtpSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_smtpSettings.SmtpUser, _smtpSettings.SmtpPass),
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Email sending failed: " + ex.Message);
            }
        }

        /// Add or update QR request and save all generated QR images
        /// </summary>//final
        public async Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // --------------------------
            // Validate required fields
            // --------------------------
            if (string.IsNullOrWhiteSpace(model.CompanyName))
                throw new Exception("CompanyName is required.");

            if (model.CompanyId <= 0)
                throw new Exception("CompanyId must be greater than 0.");

            // Auto-generate RequestId if missing
           

            if (model.QrImages == null || !model.QrImages.Any())
                throw new Exception("At least one QR image is required.");

            // --------------------------
            // Set QRValidTill to end of month
            // --------------------------
            model.QRValidTill = new DateTime(
                model.QRValidFrom.Year,
                model.QRValidFrom.Month,
                DateTime.DaysInMonth(model.QRValidFrom.Year, model.QRValidFrom.Month)
            );

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                QrCodeRequest qrRequest;

                // --------------------------
                // Insert or update main QR request
                // --------------------------
                if (model.Id > 0)
                {
                    // Update existing request
                    qrRequest = await _context.QrCodeRequest
                        .FirstOrDefaultAsync(x => x.Id == model.Id)
                        ?? throw new Exception("QR request not found.");

                    qrRequest.CompanyId = model.CompanyId;
                    qrRequest.CompanyName = model.CompanyName;
                    qrRequest.CompanyEmail = string.IsNullOrWhiteSpace(model.CompanyEmail) ? null : model.CompanyEmail;
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
                    // New request
                    qrRequest = new QrCodeRequest
                    {
                        CompanyId = model.CompanyId,
                        CompanyName = model.CompanyName,
                        CompanyEmail = string.IsNullOrWhiteSpace(model.CompanyEmail) ? null : model.CompanyEmail,
                        RequestId = model.RequestId,
                        NoofQR = model.NoofQR,
                        QRValidFrom = model.QRValidFrom,
                        QRValidTill = model.QRValidTill,
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy
                    };

                    _context.QrCodeRequest.Add(qrRequest);
                }

                await _context.SaveChangesAsync(); // Save main request to get ID

                // --------------------------
                // Remove old QR images if updating
                // --------------------------
                if (model.Id > 0)
                {
                    var existingImages = _context.QrImage
                        .Where(x => x.Qrcoderequestid == qrRequest.Id);

                    _context.QrImage.RemoveRange(existingImages);
                    await _context.SaveChangesAsync();
                }

                // --------------------------
                // Insert new QR images
                // --------------------------
                foreach (var qr in model.QrImages)
                {
                    if (string.IsNullOrWhiteSpace(qr.QrCodeImageBase64))
                        throw new Exception("QR image Base64 is required for each QR.");

                    var qrImage = new QrImage
                    {

                        Qrcoderequestid = qrRequest.Id,
                        QrCodeText = qr.QrCodeText,
                        QrCodeImage = Convert.FromBase64String(qr.QrCodeImageBase64), // store as bytes
                        IsActive = qr.IsActive,
                        SerialNo = qr.SerialNo,
                        UniqueCode = qr.UniqueCode,
                        IsUsed = false,
                        UsedDate = null,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy.ToString(),
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = model.UpdatedBy > 0 ? model.UpdatedBy.ToString() : model.CreatedBy.ToString()
                    };

                    _context.QrImage.Add(qrImage);
                }

                await _context.SaveChangesAsync(); // Save QR images
                await transaction.CommitAsync();

                // --------------------------
                // Map back ID to DTO
                // --------------------------
                model.Id = qrRequest.Id;
                return model;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
       
    }











}




    

