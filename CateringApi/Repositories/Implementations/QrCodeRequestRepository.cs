using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Common;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QRCoder;
using System.Data;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CateringApi.Repositories.Implementations
{
    public class QrCodeRequestRepository : IQrCodeRequestRepository
    {
        private readonly FoodDBContext _context;
        private readonly SmtpSettings _smtpSettings;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public QrCodeRequestRepository(
            FoodDBContext context,
            IOptions<SmtpSettings> smtpOptions,
            IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _smtpSettings = smtpOptions.Value;
            _env = env;
            _configuration = configuration;
        }
        private IDbConnection Connection =>
            new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        private static string NormalizePlanType(string? planType)
        {
            return string.IsNullOrWhiteSpace(planType) ? "Basic" : planType.Trim();
        }

        private static string GetPlanTypeShortCode(string? planType)
        {
            var value = NormalizePlanType(planType).ToUpper();

            return value switch
            {
                "BASIC" => "BSC",
                "STANDARD" => "STD",
                "PREMIUM" => "PRM",
                _ => new string(value.Where(char.IsLetterOrDigit).ToArray())
            };
        }

        public async Task<List<QrCodeRequestModel>> GetAllQRList()
        {
            var result = await (
                from qr in _context.QrCodeRequest
                join rh in _context.RequestHeader on qr.RequestId equals rh.Id
                join cm in _context.CompanyMaster on qr.CompanyId equals cm.Id
                join ro in _context.RequestOverride on qr.OverrideId equals ro.Id into roGroup
                from ro in roGroup.DefaultIfEmpty()
                where qr.IsActive
                select new QrCodeRequestModel
                {
                    Id = qr.Id,
                    CompanyId = qr.CompanyId,
                    CompanyName = cm.CompanyName,
                    CompanyEmail = cm.Email,
                    RequestId = qr.RequestId,
                    OverrideId = qr.OverrideId,
                    RequestNo = qr.OverrideId != null && ro != null
                        ? rh.RequestNo + " / OVR-" + ro.Id
                        : rh.RequestNo,
                    NoofQR = qr.NoofQR,
                    QRValidFrom = qr.QRValidFrom,
                    QRValidTill = qr.QRValidTill,
                    PlanType = !string.IsNullOrWhiteSpace(qr.PlanType) ? qr.PlanType : (rh.PlanType ?? "Basic"),
                    IsActive = qr.IsActive,
                    CreatedDate = qr.CreatedDate,
                    UpdatedDate = qr.UpdatedDate,
                    CreatedBy = qr.CreatedBy,
                    UpdatedBy = qr.UpdatedBy,

                    ApprovalStatus = qr.ApprovalStatus,
                    RequestedBy = qr.RequestedBy,
                    RequestedDate = qr.RequestedDate,
                    ApprovedBy = qr.ApprovedBy,
                    ApprovedDate = qr.ApprovedDate,
                    RejectedBy = qr.RejectedBy,
                    RejectedDate = qr.RejectedDate,
                    RejectionReason = qr.RejectionReason,

                    QrImages = _context.QrImage
                        .Where(x => x.Qrcoderequestid == qr.Id && x.IsActive)
                        .Select(x => new QrImageModel
                        {
                            Id = x.Id,
                            Qrcoderequestid = x.Qrcoderequestid,
                            QrCodeImageBase64 = x.QrCodeImage != null
                                ? Convert.ToBase64String(x.QrCodeImage)
                                : null,
                            QrCodeText = x.QrCodeText,
                            SerialNo = x.SerialNo,
                            UniqueCode = x.UniqueCode,
                            IsUsed = x.IsUsed,
                            UsedDate = x.UsedDate,
                            IsActive = x.IsActive,
                            CreatedDate = x.CreatedDate,
                            CreatedBy = x.CreatedBy,
                            UpdatedBy = x.UpdatedBy,
                            UpdatedDate = x.UpdatedDate
                        }).ToList()
                }
            )
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

            return result;
        }

        public async Task<QrCodeRequestModel> DeleteQrCode(int id, int userId)
        {
            var entity = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (entity == null)
                return null;

            entity.IsActive = false;
            entity.UpdatedBy = userId;
            entity.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new QrCodeRequestModel
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                CompanyName = entity.CompanyName,
                CompanyEmail = entity.CompanyEmail,
                RequestId = entity.RequestId,
                NoofQR = entity.NoofQR,
                QRValidFrom = entity.QRValidFrom,
                QRValidTill = entity.QRValidTill,
                PlanType = entity.PlanType,
                IsActive = entity.IsActive,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }

        private byte[] AddLogoToQr(byte[] qrBytes, string logoPath, string planType)
        {
            using var qrMs = new MemoryStream(qrBytes);
            using var qrBitmap = new Bitmap(qrMs);

            int extraHeight = 60;
            using var finalBitmap = new Bitmap(qrBitmap.Width, qrBitmap.Height + extraHeight);

            using (var graphics = Graphics.FromImage(finalBitmap))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Draw QR
                graphics.DrawImage(qrBitmap, 0, 0, qrBitmap.Width, qrBitmap.Height);

                // Draw center logo
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    using var logoBitmap = new Bitmap(logoPath);

                    int logoSize = (int)(qrBitmap.Width * 0.18);
                    int logoX = (qrBitmap.Width - logoSize) / 2;
                    int logoY = (qrBitmap.Height - logoSize) / 2;
                    int padding = 8;

                    graphics.FillEllipse(
                        Brushes.White,
                        logoX - padding,
                        logoY - padding,
                        logoSize + (padding * 2),
                        logoSize + (padding * 2));

                    graphics.DrawImage(logoBitmap, logoX, logoY, logoSize, logoSize);
                }

                // Draw Plan Type text at bottom
                string planLabel = string.IsNullOrWhiteSpace(planType) ? "BASIC" : planType.Trim().ToUpper();

                using var font = new Font("Arial", 18, FontStyle.Bold);
                using var brush = new SolidBrush(Color.Black);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                var textRect = new RectangleF(0, qrBitmap.Height, qrBitmap.Width, extraHeight);
                graphics.DrawString(planLabel, font, brush, textRect, sf);
            }

            using var outputMs = new MemoryStream();
            finalBitmap.Save(outputMs, ImageFormat.Png);
            return outputMs.ToArray();
        }

        public async Task<ApiResponse> GenerateUniqueQrs(QrCodeRequest model)
        {
            try
            {
                if (model == null || model.RequestId <= 0)
                {
                    return new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Valid request is required."
                    };
                }

                var request = await _context.RequestHeader
                    .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.IsActive);

                if (request == null)
                {
                    return new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "Request not found."
                    };
                }

                string planType = NormalizePlanType(model.PlanType ?? request.PlanType);
                int requiredQty = model.NoofQR;

                if (requiredQty <= 0)
                {
                    return new ApiResponse
                    {
                        IsSuccess = false,
                        Message = "QR quantity should be greater than 0."
                    };
                }

                var validation = await ValidateCompanyUserCountAsync(
     model.RequestId,
     model.OverrideId,
     model.PlanType
 );

                if (!validation.IsAllowed)
                {
                    return new ApiResponse
                    {
                        IsSuccess = false,
                        Message = validation.Message,
                        Data = new
                        {
                            redirectTo = "/users/list",
                            companyId = validation.CompanyId,
                            planType = planType,
                            requiredCount = validation.RequiredCount,
                            availableUserCount = validation.AvailableUserCount,
                            missingUserCount = validation.MissingUserCount
                        }
                    };
                }

                var qrResults = new List<QrResultDto>();

                string planCode = GetPlanTypeShortCode(planType);
                string logoPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Images",
                    "CSPL Logo.png"
                );

                for (int i = 1; i <= requiredQty; i++)
                {
                    string uniqueCode =
                        $"CSPL-{planCode}-REQ{model.RequestId}-QR{i:D4}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

                    string qrText = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        uniqueCode,
                        requestId = model.RequestId,
                        overrideId = model.OverrideId,
                        companyId = model.CompanyId,
                        planType = planType,
                        validFrom = model.QRValidFrom.ToString("yyyy-MM-dd"),
                        validTill = model.QRValidTill.ToString("yyyy-MM-dd")
                    });

                    using var qrGenerator = new QRCodeGenerator();
                    using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrData);

                    byte[] qrBytes = qrCode.GetGraphic(20);
                    byte[] finalBytes = AddLogoToQr(qrBytes, logoPath, planType);

                    qrResults.Add(new QrResultDto
                    {
                        Text = qrText,
                        ImageBytes = finalBytes,
                        ImageBase64 = Convert.ToBase64String(finalBytes),
                        SerialNo = i,
                        UniqueCode = uniqueCode
                    });
                }

                return new ApiResponse
                {
                    IsSuccess = true,
                    Message = $"{planType} QR codes generated successfully.",
                    Data = qrResults
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
        private async Task SendQrEmailToAssignedUsersAsync(int qrCodeRequestId)
        {
            var assignments = await (
                from a in _context.QrUserAssignment
                join qi in _context.QrImage on a.QrImageId equals qi.Id
                where a.QrCodeRequestId == qrCodeRequestId
                      && a.IsActive
                      && !a.IsEmailSent
                select new
                {
                    Assignment = a,
                    Qr = qi
                }
            ).ToListAsync();

            foreach (var item in assignments)
            {
                if (string.IsNullOrWhiteSpace(item.Assignment.Email))
                    continue;

                var payload = new SendEmailDto
                {
                    Email = item.Assignment.Email,
                    QrItems = new List<SendQrItemDto>
            {
                new SendQrItemDto
                {
                    UniqueCode = item.Qr.UniqueCode ?? "",
                    QrText = item.Qr.QrCodeText ?? "",
                    QrImageBase64 = item.Qr.QrCodeImage != null
                        ? Convert.ToBase64String(item.Qr.QrCodeImage)
                        : "",
                    SerialNo = item.Qr.SerialNo,
                    IsUsed = item.Qr.IsUsed,
                    UsedDate = item.Qr.UsedDate
                }
            }
                };

                await SendQrEmailAsync(payload);

                item.Assignment.IsEmailSent = true;
                item.Assignment.EmailSentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<QrRequestWithImagesDto?> GetQrImageDetailsByRequestId(int qrcoderequestid)
        {
            var requestData = await (
                from q in _context.QrCodeRequest
                join r in _context.RequestHeader on q.RequestId equals r.Id
                join c in _context.CompanyMaster on q.CompanyId equals c.Id into companyJoin
                from c in companyJoin.DefaultIfEmpty()
                where q.Id == qrcoderequestid
                select new QrRequestWithImagesDto
                {
                    Id = q.Id,
                    RequestId = q.RequestId,
                    CompanyId = q.CompanyId,
                    CompanyName = c != null ? c.CompanyName : null,
                    CompanyEmail = c != null ? c.Email : null,
                    RequestNo = r.RequestNo,
                    NoOfQR = q.NoofQR,
                    QrValidFrom = q.QRValidFrom,
                    QrValidTill = q.QRValidTill
                }
            ).FirstOrDefaultAsync();

            if (requestData == null)
                return null;

            var qrImages = await _context.QrImage
                .Where(x => x.Qrcoderequestid == requestData.Id && x.IsActive)
                .OrderBy(x => x.SerialNo)
                .Select(x => new QrImageItemDto
                {
                    Id = x.Id,
                    QrCodeRequestId = x.Qrcoderequestid,
                    QrCodeText = x.QrCodeText,
                    SerialNo = x.SerialNo,
                    UniqueCode = x.UniqueCode,
                    IsUsed = x.IsUsed,
                    UsedDate = x.UsedDate,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate,
                    QrCodeImageBase64 = x.QrCodeImage != null
                        ? Convert.ToBase64String(x.QrCodeImage)
                        : null
                })
                .ToListAsync();

            requestData.QrImages = qrImages;

            return requestData;
        }

        public async Task<(byte[] ZipBytes, string FileName)?> DownloadQrZip(int qrcoderequestid)
        {
            var data = await GetQrImageDetailsByRequestId(qrcoderequestid);

            if (data == null || data.QrImages == null || !data.QrImages.Any())
                return null;

            var validImages = data.QrImages
                .Where(x => !string.IsNullOrWhiteSpace(x.QrCodeImageBase64))
                .ToList();

            if (!validImages.Any())
                return null;

            var requestLabel = string.IsNullOrWhiteSpace(data.RequestNo)
                ? "qr"
                : SanitizeFileName(data.RequestNo);

            var companyLabel = string.IsNullOrWhiteSpace(data.CompanyName)
                ? "qr-images"
                : SanitizeFileName(data.CompanyName);

            var qrRequest = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == qrcoderequestid);

            string planLabel = qrRequest?.PlanType?.Trim().ToUpper() ?? "BASIC";

            using var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                int addedCount = 0;

                foreach (var img in validImages)
                {
                    try
                    {
                        var base64 = img.QrCodeImageBase64!.Trim();

                        if (base64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                        {
                            var commaIndex = base64.IndexOf(',');
                            if (commaIndex >= 0)
                                base64 = base64[(commaIndex + 1)..];
                        }

                        var imageBytes = Convert.FromBase64String(base64);

                        if (imageBytes.Length == 0)
                            continue;

                        var serialNo = img.SerialNo ?? (addedCount + 1);

                        var entry = archive.CreateEntry($"{requestLabel}-{planLabel}.png");

                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(imageBytes, 0, imageBytes.Length);

                        addedCount++;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (addedCount == 0)
                    return null;
            }

            // 🔥 VERY IMPORTANT (after archive disposed)
            var zipBytes = memoryStream.ToArray();

            var zipFileName = $"CSPL-{companyLabel}-{planLabel}.zip";

            return (zipBytes, zipFileName);
        }

        private string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "file";

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '-');
            }

            return value.Replace("/", "-")
                        .Replace("\\", "-")
                        .Trim();
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
  <title>Your QR Codes</title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif; color:#333333;'>
  <div style='width:100%; background-color:#f4f6f8; padding:30px 0;'>
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width:900px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 14px rgba(0,0,0,0.08);'>
      <tr>
        <td style='background: linear-gradient(135deg, #b7410e, #e25822); padding:20px 32px;'>
          <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='width:100%; border-collapse:collapse;'>
            <tr>
              <td style='width:110px; vertical-align:middle; text-align:left;'>
                <div style='display:inline-block; background-color:#ffffff; padding:10px; border-radius:12px;'>
                  <img src='cid:csplLogo'
                       alt='CSPL Logo'
                       style='display:block; width:80px; height:80px; object-fit:contain;' />
                </div>
              </td>
              <td style='vertical-align:middle; text-align:center;'>
                <h1 style='margin:0;font-size:40px;color:#ffffff;font-weight:700;letter-spacing:1.5px;font-family:Cinzel, serif;'>
                  CSPL
                </h1>
              </td>
              <td style='width:110px;'>&nbsp;</td>
            </tr>
          </table>
        </td>
      </tr>

      <tr>
        <td style='padding:28px 32px 12px;background: linear-gradient(135deg, #f4e1c1, #e6c79c);'>
          <p style='margin:0 0 16px; font-size:15px; line-height:1.6; color:#4b5563;'>Hello,</p>
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
            </tr>
");

                int count = 1;

                foreach (var qr in model.QrItems)
                {
                    string rowBg = count % 2 == 0 ? "#fcfcfd" : "#ffffff";

                    bodyBuilder.Append($@"
            <tr style='background-color:{rowBg};'>
              <td style='padding:12px; border:1px solid #e5e7eb; font-size:14px; color:#374151; text-align:center;'>{count}</td>
              <td style='padding:12px; border:1px solid #e5e7eb; font-size:14px; color:#111827; font-weight:600; text-align:left;'>{qr.UniqueCode}</td>
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

                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "CSPL Logo.png");
                string logoContentId = "csplLogo";

                if (File.Exists(logoPath))
                {
                    var htmlView = AlternateView.CreateAlternateViewFromString(bodyBuilder.ToString(), null, "text/html");

                    var linkedLogo = new LinkedResource(logoPath, "image/png")
                    {
                        ContentId = logoContentId,
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };

                    htmlView.LinkedResources.Add(linkedLogo);
                    message.AlternateViews.Add(htmlView);
                }
                else
                {
                    message.Body = bodyBuilder.ToString();
                }

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

        public async Task<List<RequestDropdownDto>> GetQrPendingDropdown()
        {
            var result = new List<RequestDropdownDto>();

            var baseRequests = await (
                from rh in _context.RequestHeader
                join cm in _context.CompanyMaster on rh.CompanyId equals cm.Id
                where rh.IsActive
                select new
                {
                    rh.Id,
                    rh.RequestNo,
                    rh.CompanyId,
                    cm.CompanyName,
                    cm.Email,
                    rh.FromDate,
                    rh.ToDate
                }
            ).ToListAsync();

            foreach (var req in baseRequests)
            {
                // BASE REQUEST
                var baseData = await (
                    from rd in _context.RequestDetail
                    join cu in _context.CuisineMaster on rd.CuisineId equals cu.Id into cuJoin
                    from cu in cuJoin.DefaultIfEmpty()
                    where rd.RequestHeaderId == req.Id
                          && rd.IsActive
                          && rd.Qty > 0
                    group rd by new
                    {
                        PlanType = string.IsNullOrWhiteSpace(rd.PlanType)
                            ? "Basic"
                            : rd.PlanType.Trim(),

                        rd.CuisineId,

                        CuisineName = cu != null
                            ? cu.CuisineName
                            : ""
                    }
                    into g
                    select new
                    {
                        g.Key.PlanType,
                        g.Key.CuisineId,
                        g.Key.CuisineName,
                        Qty = g.Sum(x => x.Qty)
                    }
                ).ToListAsync();

                var groupedPlan = baseData
                    .GroupBy(x => x.PlanType)
                    .ToList();

                foreach (var planGroup in groupedPlan)
                {
                    var planType = planGroup.Key;
                    var totalQty = planGroup.Sum(x => x.Qty);

                    decimal approvedQty = await _context.QrCodeRequest
                        .Where(x =>
                            x.RequestId == req.Id &&
                            x.OverrideId == null &&
                            x.IsActive &&
                            x.ApprovalStatus == 1 &&
                            x.PlanType == planType)
                        .SumAsync(x => (decimal?)x.NoofQR) ?? 0;

                    var pendingQty = totalQty - approvedQty;

                    if (pendingQty <= 0)
                        continue;

                    var cuisines = planGroup
                        .Select(x => new CuisineDropdownDto
                        {
                            CuisineId = x.CuisineId,
                            CuisineName = x.CuisineName,
                            Qty = x.Qty
                        })
                        .ToList();

                    var cuisineSummary = string.Join(", ",
                        cuisines.Select(x => $"{x.CuisineName}({x.Qty})"));

                    result.Add(new RequestDropdownDto
                    {
                        RequestId = req.Id,
                        OverrideId = null,
                        RequestNo = req.RequestNo,
                        CompanyId = req.CompanyId,
                        CompanyName = req.CompanyName,
                        CompanyEmail = req.Email,
                        Qty = pendingQty,
                        FromDate = req.FromDate,
                        TillDate = req.ToDate,
                        SourceType = "REQUEST_PENDING",
                        PlanType = planType,

                        // CuisineId + CuisineName list
                        Cuisines = cuisines,

                        DisplayText =
                            $"{req.RequestNo} - {req.CompanyName} - {planType} - {cuisineSummary} - Qty {pendingQty}"
                    });
                }

                // OVERRIDE REQUEST
                var overrides = await _context.RequestOverride
                    .Where(x => x.RequestHeaderId == req.Id && x.IsActive)
                    .ToListAsync();

                foreach (var ov in overrides)
                {
                    var overrideData = await (
                        from rod in _context.RequestOverrideDetail
                        join rd in _context.RequestDetail on rod.RequestDetailId equals rd.Id
                        join cu in _context.CuisineMaster on rd.CuisineId equals cu.Id into cuJoin
                        from cu in cuJoin.DefaultIfEmpty()
                        where rod.RequestOverrideId == ov.Id
                              && rod.IsActive
                              && !rod.IsCancelled
                              && rod.OverrideQty > 0
                        group rod by new
                        {
                            PlanType = string.IsNullOrWhiteSpace(rd.PlanType)
                                ? "Basic"
                                : rd.PlanType.Trim(),

                            rd.CuisineId,

                            CuisineName = cu != null
                                ? cu.CuisineName
                                : ""
                        }
                        into g
                        select new
                        {
                            g.Key.PlanType,
                            g.Key.CuisineId,
                            g.Key.CuisineName,
                            Qty = g.Sum(x => x.OverrideQty)
                        }
                    ).ToListAsync();

                    var groupedOverride = overrideData
                        .GroupBy(x => x.PlanType)
                        .ToList();

                    foreach (var planGroup in groupedOverride)
                    {
                        var planType = planGroup.Key;
                        var totalQty = planGroup.Sum(x => x.Qty);

                        decimal approvedQty = await _context.QrCodeRequest
                            .Where(x =>
                                x.RequestId == req.Id &&
                                x.OverrideId == ov.Id &&
                                x.IsActive &&
                                x.ApprovalStatus == 1 &&
                                x.PlanType == planType)
                            .SumAsync(x => (decimal?)x.NoofQR) ?? 0;

                        var pendingQty = totalQty - approvedQty;

                        if (pendingQty <= 0)
                            continue;

                        var cuisines = planGroup
                            .Select(x => new CuisineDropdownDto
                            {
                                CuisineId = x.CuisineId,
                                CuisineName = x.CuisineName,
                                Qty = x.Qty
                            })
                            .ToList();

                        var cuisineSummary = string.Join(", ",
                            cuisines.Select(x => $"{x.CuisineName}({x.Qty})"));

                        result.Add(new RequestDropdownDto
                        {
                            RequestId = req.Id,
                            OverrideId = ov.Id,
                            RequestNo = req.RequestNo,
                            CompanyId = req.CompanyId,
                            CompanyName = req.CompanyName,
                            CompanyEmail = req.Email,
                            Qty = pendingQty,
                            FromDate = ov.FromDate,
                            TillDate = ov.ToDate,
                            SourceType = "OVERRIDE_PENDING",
                            PlanType = planType,

                            // CuisineId + CuisineName list
                            Cuisines = cuisines,

                            DisplayText =
                                $"{req.RequestNo} - {req.CompanyName} - {planType} - {cuisineSummary} - Override #{ov.Id} - Qty {pendingQty}"
                        });
                    }
                }
            }

            return result
                .Where(x => x.Qty > 0)
                .OrderBy(x => x.RequestNo)
                .ThenBy(x => x.FromDate)
                .ThenBy(x => x.PlanType)
                .ToList();
        }


        public async Task<ApiResponseDto> AddUpdateQrWithImagesAsync(QrCodeRequestModel model)
        {
            if (model == null)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid request data."
                };
            }

            if (model.CompanyId <= 0)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "CompanyId must be greater than 0."
                };
            }

            if (model.NoofQR <= 0)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "NoofQR must be greater than 0."
                };
            }

            var availableUserCount = await _context.UserMaster
                .CountAsync(x => x.CompanyId == model.CompanyId && x.IsActive);

            var missingUserCount = model.NoofQR > availableUserCount
                ? model.NoofQR - availableUserCount
                : 0;

            if (missingUserCount > 0)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = $"QR cannot be generated. Required users: {model.NoofQR}, available users: {availableUserCount}. {missingUserCount} user(s) not available.",
                    Data = new
                    {
                        companyId = model.CompanyId,
                        requiredCount = model.NoofQR,
                        availableUserCount = availableUserCount,
                        missingUserCount = missingUserCount
                    }
                };
            }

            // normal save logic here

            return new ApiResponseDto
            {
                IsSuccess = true,
                Message = "QR request submitted for approval successfully."
            };
        }
        public async Task<ApiResponseDto> SubmitQrApprovalRequestAsync(QrCodeRequestModel model)
        {
            try
            {
                if (model == null)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid request data.",
                        MessageType = "error"
                    };
                }

                if (model.CompanyId <= 0)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Company is required.",
                        MessageType = "warning"
                    };
                }

                if (model.RequestId <= 0)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Request is required.",
                        MessageType = "warning"
                    };
                }

                if (model.NoofQR <= 0)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "No of QR should be greater than 0.",
                        MessageType = "warning"
                    };
                }

                int? safeOverrideId = (model.OverrideId.HasValue && model.OverrideId.Value > 0)
                    ? model.OverrideId.Value
                    : null;

                // STEP 1: Validate available users before submit for approval
                var validation = await ValidateCompanyUserCountAsync(
     model.RequestId,
     safeOverrideId,
     model.PlanType
 );

                if (!validation.IsAllowed)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = validation.Message,
                        MessageType = "warning",
                        Data = new
                        {
                            redirectTo = "/users/list",
                            companyId = validation.CompanyId,
                            planType = validation.PlanType,
                            cuisineId = validation.CuisineId,
                            cuisineName = validation.CuisineName,
                            requiredCount = validation.RequiredCount,
                            availableUserCount = validation.AvailableUserCount,
                            missingUserCount = validation.MissingUserCount
                        }
                    };
                }

                // STEP 2: Prevent duplicate pending request for same segment
                var alreadyPending = await _context.QrCodeRequest.AnyAsync(x =>
                    x.IsActive &&
                    x.ApprovalStatus == 0 &&
                    x.RequestId == model.RequestId &&
                    x.CompanyId == model.CompanyId &&
                    (x.OverrideId ?? 0) == (safeOverrideId ?? 0) &&
                    x.QRValidFrom.Date == model.QRValidFrom.Date &&
                    x.QRValidTill.Date == model.QRValidTill.Date);

                if (alreadyPending)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Approval request already pending for this segment.",
                        MessageType = "warning"
                    };
                }

                // STEP 3: Prevent already-approved same segment duplicate
                var alreadyApproved = await _context.QrCodeRequest.AnyAsync(x =>
                    x.IsActive &&
                    x.ApprovalStatus == 1 &&
                    x.RequestId == model.RequestId &&
                    x.CompanyId == model.CompanyId &&
                    (x.OverrideId ?? 0) == (safeOverrideId ?? 0) &&
                    x.QRValidFrom.Date == model.QRValidFrom.Date &&
                    x.QRValidTill.Date == model.QRValidTill.Date);

                if (alreadyApproved)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "This segment is already approved.",
                        MessageType = "warning"
                    };
                }

                var entity = new QrCodeRequest
                {
                    CompanyId = model.CompanyId,
                    CompanyName = model.CompanyName,
                    CompanyEmail = model.CompanyEmail,
                    RequestId = model.RequestId,
                    OverrideId = safeOverrideId,
                    NoofQR = model.NoofQR,
                    QRValidFrom = model.QRValidFrom,
                    QRValidTill = model.QRValidTill,
                    PlanType = model.PlanType,
                    IsActive = true,
                    ApprovalStatus = 0,
                    RequestedBy = model.CreatedBy,
                    RequestedDate = DateTime.UtcNow,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.QrCodeRequest.Add(entity);
                await _context.SaveChangesAsync();

                return new ApiResponseDto
                {
                    IsSuccess = true,
                    Message = "QR request submitted for approval successfully.",
                    MessageType = "success",
                    Data = entity.Id
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    MessageType = "error"
                };
            }
        }

        public async Task<ApiResponseDto> ApproveQrRequestAsync(int qrCodeRequestId, int approvedBy)
        {
            if (qrCodeRequestId <= 0)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "Valid QR request id is required.",
                    MessageType = "error"
                };
            }

            if (approvedBy <= 0)
            {
                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = "Valid approvedBy is required.",
                    MessageType = "error"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var qrRequest = await _context.QrCodeRequest
                    .FirstOrDefaultAsync(x => x.Id == qrCodeRequestId && x.IsActive);

                if (qrRequest == null)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "QR request not found.",
                        MessageType = "error"
                    };
                }

                if (qrRequest.ApprovalStatus != 0)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "Only pending QR requests can be approved.",
                        MessageType = "error"
                    };
                }

                var existingApproved = await _context.QrCodeRequest.AnyAsync(x =>
                    x.Id != qrRequest.Id &&
                    x.IsActive &&
                    x.ApprovalStatus == 1 &&
                    x.RequestId == qrRequest.RequestId &&
                    (x.OverrideId ?? 0) == (qrRequest.OverrideId ?? 0) &&
                    x.QRValidFrom.Date == qrRequest.QRValidFrom.Date &&
                    x.QRValidTill.Date == qrRequest.QRValidTill.Date);

                if (existingApproved)
                {
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "This QR request is already approved for the selected segment.",
                        MessageType = "warning"
                    };
                }

                var generateModel = new QrCodeRequest
                {
                    Id = qrRequest.Id,
                    CompanyId = qrRequest.CompanyId,
                    CompanyName = qrRequest.CompanyName,
                    CompanyEmail = qrRequest.CompanyEmail,
                    RequestId = qrRequest.RequestId,
                    OverrideId = qrRequest.OverrideId,
                    NoofQR = qrRequest.NoofQR,
                    QRValidFrom = qrRequest.QRValidFrom,
                    QRValidTill = qrRequest.QRValidTill,
                    PlanType = qrRequest.PlanType,
                    IsActive = qrRequest.IsActive,
                    CreatedBy = qrRequest.CreatedBy,
                    UpdatedBy = approvedBy
                };

                var qrResponse = await GenerateUniqueQrs(generateModel);

                if (qrResponse == null)
                {
                    await transaction.RollbackAsync();
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "QR generation failed.",
                        MessageType = "error"
                    };
                }

                // IMPORTANT
                if (!qrResponse.IsSuccess)
                {
                    await transaction.RollbackAsync();

                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = qrResponse.Message,
                        MessageType = "warning",
                        Data = qrResponse.Data
                    };
                }

                var qrResults = qrResponse.Data as List<QrResultDto>;

                if (qrResults == null || !qrResults.Any())
                {
                    await transaction.RollbackAsync();
                    return new ApiResponseDto
                    {
                        IsSuccess = false,
                        Message = "QR generation failed.",
                        MessageType = "error"
                    };
                }

                var oldImages = await _context.QrImage
                    .Where(x => x.Qrcoderequestid == qrRequest.Id && x.IsActive)
                    .ToListAsync();

                foreach (var old in oldImages)
                {
                    old.IsActive = false;
                    old.UpdatedDate = DateTime.UtcNow;
                    old.UpdatedBy = approvedBy.ToString();
                }

                foreach (var qr in qrResults)
                {
                    byte[] imageBytes;

                    if (!string.IsNullOrWhiteSpace(qr.ImageBase64))
                    {
                        imageBytes = Convert.FromBase64String(qr.ImageBase64);
                    }
                    else if (qr.ImageBytes != null && qr.ImageBytes.Length > 0)
                    {
                        imageBytes = qr.ImageBytes;
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return new ApiResponseDto
                        {
                            IsSuccess = false,
                            Message = "Generated QR image is empty.",
                            MessageType = "error"
                        };
                    }

                    var qrImage = new QrImage
                    {
                        Qrcoderequestid = qrRequest.Id,
                        QrCodeText = qr.Text,
                        QrCodeImage = imageBytes,
                        IsActive = true,
                        SerialNo = qr.SerialNo,
                        UniqueCode = qr.UniqueCode,
                        PlanType = qrRequest.PlanType,
                        IsUsed = false,
                        UsedDate = null,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = approvedBy.ToString(),
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = approvedBy.ToString()
                    };

                    _context.QrImage.Add(qrImage);
                    await _context.SaveChangesAsync();

                    var assignedUserIds = await _context.QrUserAssignment
      .Where(x => x.QrCodeRequestId == qrRequest.Id && x.IsActive)
      .Select(x => x.UserId)
      .ToListAsync();

                    string planType = NormalizePlanType(qrRequest.PlanType).ToUpper();

                    var user = await _context.UserMaster
                        .Where(x =>
                            x.CompanyId == qrRequest.CompanyId &&
                            x.IsActive &&
                            !x.IsDelete &&
                            !assignedUserIds.Contains(x.Id) &&
                            (
                                ((x.PlanType == null || x.PlanType == "") && planType == "BASIC") ||
                                x.PlanType.ToUpper() == planType
                            ))
                        .OrderBy(x => x.Id)
                        .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        await transaction.RollbackAsync();

                        return new ApiResponseDto
                        {
                            IsSuccess = false,
                            Message = $"No available {planType} user found for QR assignment.",
                            MessageType = "warning"
                        };
                    }

                    _context.QrUserAssignment.Add(new QrUserAssignment
                    {
                        QrCodeRequestId = qrRequest.Id,
                        QrImageId = qrImage.Id,
                        UserId = user.Id,
                        CompanyId = qrRequest.CompanyId,
                        RequestId = qrRequest.RequestId,
                        OverrideId = qrRequest.OverrideId,
                        PlanType = planType,
                        UniqueCode = qr.UniqueCode ?? "",
                        Email = user.Email ?? "",
                        IsEmailSent = false,
                        EmailSentDate = null,
                        IsActive = true,
                        CreatedBy = approvedBy,
                        CreatedDate = DateTime.UtcNow
                    });
                }

                qrRequest.ApprovalStatus = 1;
                qrRequest.ApprovedBy = approvedBy;
                qrRequest.ApprovedDate = DateTime.UtcNow;
                qrRequest.UpdatedBy = approvedBy;
                qrRequest.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await SendQrEmailToAssignedUsersAsync(qrRequest.Id);

                return new ApiResponseDto
                {
                    IsSuccess = true,
                    Message = "QR request approved successfully. QR generated and sent to users.",
                    MessageType = "success"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new ApiResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    MessageType = "error"
                };
            }
        }

        public async Task<string> RejectQrRequestAsync(int qrCodeRequestId, int rejectedBy, string reason)
        {
            if (qrCodeRequestId <= 0)
                throw new Exception("Valid QR request id is required.");

            if (rejectedBy <= 0)
                throw new Exception("Valid rejectedBy is required.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection reason is required.");

            var qrRequest = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == qrCodeRequestId && x.IsActive);

            if (qrRequest == null)
                throw new Exception("QR request not found.");

            if (qrRequest.ApprovalStatus != 0)
                throw new Exception("Only pending QR requests can be rejected.");

            qrRequest.ApprovalStatus = 2;
            qrRequest.RejectedBy = rejectedBy;
            qrRequest.RejectedDate = DateTime.UtcNow;
            qrRequest.RejectionReason = reason.Trim();
            qrRequest.UpdatedBy = rejectedBy;
            qrRequest.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return "QR request rejected successfully.";
        }

        private async Task SendQrEmailToCompanyAsync(int qrCodeRequestId)
        {
            var request = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == qrCodeRequestId && x.IsActive);

            if (request == null)
                throw new Exception("QR request not found.");

            if (string.IsNullOrWhiteSpace(request.CompanyEmail))
                throw new Exception("Company email not found.");

            var qrImages = await _context.QrImage
                .Where(x => x.Qrcoderequestid == qrCodeRequestId && x.IsActive)
                .OrderBy(x => x.SerialNo)
                .ToListAsync();

            if (!qrImages.Any())
                throw new Exception("No QR images found to send email.");

            var emailPayload = new SendEmailDto
            {
                Email = request.CompanyEmail,
                QrItems = qrImages.Select(x => new SendQrItemDto
                {
                    UniqueCode = x.UniqueCode ?? "",
                    QrText = x.QrCodeText ?? "",
                    QrImageBase64 = x.QrCodeImage != null
                        ? Convert.ToBase64String(x.QrCodeImage)
                        : "",
                    SerialNo = x.SerialNo,
                    IsUsed = x.IsUsed,
                    UsedDate = x.UsedDate
                }).ToList()
            };

            await SendQrEmailAsync(emailPayload);
        }
        public async Task<QrUserCountValidationDto> ValidateCompanyUserCountAsync(
      int requestId,
      int? overrideId,
      string? planType)
        {
            using var con = Connection;

            var result = await con.QueryFirstOrDefaultAsync<QrUserCountValidationDto>(
                "dbo.sp_Qr_ValidateCompanyUserCount",
                new
                {
                    RequestId = requestId,
                    OverrideId = overrideId,
                    PlanType = planType
                },
                commandType: CommandType.StoredProcedure);

            return result ?? new QrUserCountValidationDto
            {
                IsAllowed = false,
                Message = "Unable to validate company user count.",
                CompanyId = 0,
                PlanType = planType ?? "Basic",
                RequiredCount = 0,
                AvailableUserCount = 0,
                MissingUserCount = 0
            };
        }
        public async Task<List<QrTargetUserDto>> GetQrTargetUsersAsync(
      int companyId,
      string planType,
      int count,
         int cuisineId)
        {
            planType = string.IsNullOrWhiteSpace(planType) ? "Basic" : planType.Trim();

            return await _context.UserMaster
                .Where(x =>
                    x.CompanyId == companyId &&
                    x.IsActive &&
                    !x.IsDelete &&
                    x.CuisineId == cuisineId &&
                    !string.IsNullOrWhiteSpace(x.Email) &&
                    (
                        ((x.PlanType == null || x.PlanType == "") && planType.ToUpper() == "BASIC") ||
                        x.PlanType.ToUpper() == planType.ToUpper()
                    ))
                .OrderBy(x => x.Id)
                .Take(count)
                .Select(x => new QrTargetUserDto
                {
                    Id = x.Id,
                    Username = x.Username ?? "",
                    Email = x.Email ?? "",
                    PlanType = string.IsNullOrWhiteSpace(x.PlanType) ? "Basic" : x.PlanType,
                    CuisineId = x.CuisineId
                })
                .ToListAsync();
        }

    }
}