using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Master;
using CateringApi.DTOs.Scanner;
using CateringApi.DTOs.Scanner.CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;

namespace CateringApi.Repositories.Implementations
{
    public class QrCodeRequestRepository : IQrCodeRequestRepository
    {
        private readonly FoodDBContext _context;
        private readonly SmtpSettings _smtpSettings;
        private readonly IWebHostEnvironment _env;

        public QrCodeRequestRepository(
            FoodDBContext context,
            IOptions<SmtpSettings> smtpOptions,
            IWebHostEnvironment env)
        {
            _context = context;
            _smtpSettings = smtpOptions.Value;
            _env = env;
        }

        public async Task<List<QrCodeRequestModel>> GetAllQRList()
        {
            var result = await (
                from qr in _context.QrCodeRequest
                join rh in _context.RequestHeader on qr.RequestId equals rh.Id
                join cm in _context.CompanyMaster on qr.CompanyId equals cm.Id
                join ro in _context.RequestOverride on qr.OverrideId equals ro.Id into roGroup
                from ro in roGroup.DefaultIfEmpty()
                where qr.IsActive == true
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
                IsActive = entity.IsActive,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }

        private byte[] AddLogoToQr(byte[] qrBytes, string logoPath)
        {
            if (!File.Exists(logoPath))
                return qrBytes;

            using var qrMs = new MemoryStream(qrBytes);
            using var qrBitmap = new Bitmap(qrMs);
            using var logoBitmap = new Bitmap(logoPath);
            using var finalBitmap = new Bitmap(qrBitmap.Width, qrBitmap.Height);

            using (var graphics = Graphics.FromImage(finalBitmap))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                graphics.DrawImage(qrBitmap, 0, 0, qrBitmap.Width, qrBitmap.Height);

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

            using var outputMs = new MemoryStream();
            finalBitmap.Save(outputMs, ImageFormat.Png);
            return outputMs.ToArray();
        }

        public async Task<List<QrResultDto>> GenerateUniqueQrs(QrCodeRequest model)
        {
            var result = new List<QrResultDto>();

            if (model == null)
                throw new Exception("Request payload is required.");

            if (model.RequestId <= 0)
                throw new Exception("Valid RequestId is required.");

            if (model.CompanyId <= 0)
                throw new Exception("Valid CompanyId is required.");

            if (string.IsNullOrWhiteSpace(model.CompanyName))
                throw new Exception("CompanyName is required.");

            if (model.NoofQR <= 0)
                throw new Exception("NoofQR must be greater than 0.");

            DateTime validFrom = model.QRValidFrom.Date;
            DateTime validTill = model.QRValidTill.Date;

            if (validFrom > validTill)
                throw new Exception("QRValidFrom cannot be greater than QRValidTill.");

            var requestHeader = await _context.RequestHeader
                .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.IsActive);

            if (requestHeader == null)
                throw new Exception("Request not found.");

            var pendingItems = await GetQrPendingDropdown();

            var matchedPending = pendingItems.FirstOrDefault(x =>
                x.RequestId == model.RequestId &&
                (x.OverrideId ?? 0) == (model.OverrideId ?? 0) &&
                x.FromDate.Date == validFrom &&
                x.TillDate.Date == validTill);

            if (matchedPending == null)
                throw new Exception("Selected pending segment is not available. Please refresh and try again.");

            if (model.NoofQR > matchedPending.Qty)
                throw new Exception($"Only {matchedPending.Qty} QR(s) can be generated for this segment.");

            int totalQty = model.NoofQR;

            string safeCompanyName = new string((model.CompanyName ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToUpper();

            string requestPart = $"RQ{model.RequestId}";
            string overridePart = (model.OverrideId ?? 0) > 0
                ? $"OVR{model.OverrideId.Value}"
                : "";

            string datePart = $"{validFrom:ddMMyyyy}{validTill:ddMMyyyy}";
            string logoPath = Path.Combine(_env.WebRootPath, "Images", "CSPL Logo.png");

            int alreadyGeneratedCount = await (
                from qi in _context.QrImage
                join qr in _context.QrCodeRequest on qi.Qrcoderequestid equals qr.Id
                where qi.IsActive
                      && qr.IsActive
                      && qr.ApprovalStatus == 1
                      && qr.RequestId == model.RequestId
                      && (qr.OverrideId ?? 0) == (model.OverrideId ?? 0)
                      && qr.QRValidFrom.Date == validFrom
                      && qr.QRValidTill.Date == validTill
                select qi.Id
            ).CountAsync();

            using var qrGenerator = new QRCodeGenerator();

            for (int i = 1; i <= totalQty; i++)
            {
                int runningSerial = alreadyGeneratedCount + i;

                string uniqueCode = !string.IsNullOrWhiteSpace(overridePart)
                    ? $"CSPL{safeCompanyName}{requestPart}{overridePart}{datePart}_{runningSerial:000}"
                    : $"CSPL{safeCompanyName}{requestPart}{datePart}_{runningSerial:000}";

                string qrText = uniqueCode;

                using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.H);
                var qrCode = new PngByteQRCode(qrData);

                byte[] qrBytes = qrCode.GetGraphic(20);
                byte[] finalQrBytes = AddLogoToQr(qrBytes, logoPath);

                result.Add(new QrResultDto
                {
                    Text = qrText,
                    ImageBytes = finalQrBytes,
                    ImageBase64 = Convert.ToBase64String(finalQrBytes),
                    UniqueCode = uniqueCode,
                    SerialNo = runningSerial,
                    UsedDate = null,
                    IsUsed = false
                });
            }

            return result;
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

                        base64 = base64.Replace(" ", "")
                                       .Replace("\r", "")
                                       .Replace("\n", "");

                        var imageBytes = Convert.FromBase64String(base64);

                        if (imageBytes.Length == 0)
                            continue;

                        var serialNo = img.SerialNo.HasValue
                            ? img.SerialNo.Value.ToString()
                            : (addedCount + 1).ToString();

                        var entryFileName = $"{requestLabel}-{serialNo}.png";

                        var entry = archive.CreateEntry(entryFileName, CompressionLevel.Fastest);

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

            memoryStream.Position = 0;

            var zipFileName = $"CSPL-{companyLabel}-{requestLabel}.zip";
            return (memoryStream.ToArray(), zipFileName);
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
  <h1 style=""
  margin:0;
  font-size:40px;
  color:#ffffff;
  font-weight:700;
  letter-spacing:1.5px;
  font-family: ''Cinzel'', serif;
"">
  CSPL
</h1>
    
  </td>

  <td style='width:110px;'>
    &nbsp;
  </td>
</tr>
          </table>
        </td>
      </tr>

      <tr>
        <td style='padding:28px 32px 12px;background: linear-gradient(135deg, #f4e1c1, #e6c79c);'>
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

        private static int NormalizeOverrideId(int? overrideId)
        {
            return overrideId.HasValue && overrideId.Value > 0 ? overrideId.Value : 0;
        }

        private static bool IsWithinRange(DateTime targetFrom, DateTime targetTill, DateTime rangeFrom, DateTime rangeTill)
        {
            return rangeFrom.Date <= targetFrom.Date && rangeTill.Date >= targetTill.Date;
        }

        public async Task<List<RequestDropdownDto>> GetQrPendingDropdown()
        {
            var result = new List<RequestDropdownDto>();

            var requests = await (
                from r in _context.RequestHeader
                join c in _context.CompanyMaster on r.CompanyId equals c.Id
                where r.IsActive
                select new
                {
                    RequestId = r.Id,
                    RequestNo = r.RequestNo,
                    CompanyId = c.Id,
                    CompanyName = c.CompanyName,
                    CompanyEmail = c.Email,
                    BaseFromDate = r.FromDate,
                    BaseToDate = r.ToDate,
                    BaseQty = (int?)r.TotalQty
                }
            ).ToListAsync();

            foreach (var req in requests)
            {
                DateTime reqFrom = req.BaseFromDate.Date;
                DateTime reqTo = req.BaseToDate.Date;
                int baseQty = req.BaseQty ?? 0;

                var overrides = await _context.RequestOverride
                    .Where(x => x.RequestHeaderId == req.RequestId && x.IsActive)
                    .OrderBy(x => x.FromDate)
                    .ThenBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        FromDate = x.FromDate.Date,
                        ToDate = x.ToDate.Date,
                        TotalQty = (int?)x.TotalQty,
                        DifferentQty = (int?)x.DifferentQty
                    })
                    .ToListAsync();

                var qrBatches = await _context.QrCodeRequest
                    .Where(x => x.RequestId == req.RequestId && x.IsActive && x.ApprovalStatus == 1)
                    .Select(x => new
                    {
                        x.Id,
                        x.CompanyId,
                        QRValidFrom = x.QRValidFrom.Date,
                        QRValidTill = x.QRValidTill.Date,
                        NoofQR = (int?)x.NoofQR
                    })
                    .ToListAsync();

                var boundaries = new List<DateTime>
        {
            reqFrom,
            reqTo.AddDays(1)
        };

                foreach (var ov in overrides)
                {
                    boundaries.Add(ov.FromDate);
                    boundaries.Add(ov.ToDate.AddDays(1));
                }

                foreach (var qr in qrBatches)
                {
                    boundaries.Add(qr.QRValidFrom);
                    boundaries.Add(qr.QRValidTill.AddDays(1));
                }

                boundaries = boundaries
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                for (int i = 0; i < boundaries.Count - 1; i++)
                {
                    DateTime segFrom = boundaries[i].Date;
                    DateTime segTill = boundaries[i + 1].Date.AddDays(-1);

                    if (segFrom > segTill)
                        continue;

                    if (segFrom < reqFrom || segTill > reqTo)
                        continue;

                    var appliedOverride = overrides
                        .Where(x => x.FromDate <= segFrom && x.ToDate >= segTill)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault();

                    int totalGeneratedQty = qrBatches
                        .Where(x =>
                            x.CompanyId == req.CompanyId &&
                            x.QRValidFrom <= segFrom &&
                            x.QRValidTill >= segTill)
                        .Sum(x => x.NoofQR ?? 0);

                    int pendingQty = 0;
                    string sourceType = "REQUEST_PENDING";
                    int? effectiveOverrideId = null;

                    if (appliedOverride != null)
                    {
                        effectiveOverrideId = appliedOverride.Id;
                        sourceType = "OVERRIDE_PENDING";

                        int extraQty = appliedOverride.DifferentQty ?? 0;
                        int overrideGeneratedQty = Math.Max(0, totalGeneratedQty - baseQty);
                        pendingQty = extraQty - overrideGeneratedQty;
                    }
                    else
                    {
                        pendingQty = baseQty - totalGeneratedQty;
                    }

                    if (pendingQty <= 0)
                        continue;

                    result.Add(new RequestDropdownDto
                    {
                        RequestId = req.RequestId,
                        OverrideId = effectiveOverrideId,
                        RequestNo = req.RequestNo,
                        CompanyId = req.CompanyId,
                        CompanyName = req.CompanyName,
                        CompanyEmail = req.CompanyEmail,
                        Qty = pendingQty,
                        FromDate = segFrom,
                        TillDate = segTill,
                        SourceType = sourceType,
                        DisplayText = effectiveOverrideId.HasValue
                            ? $"{req.RequestNo} - {req.CompanyName} - Override #{effectiveOverrideId} - {segFrom:dd-MM-yyyy} to {segTill:dd-MM-yyyy} - Qty {pendingQty}"
                            : $"{req.RequestNo} - {req.CompanyName} - {segFrom:dd-MM-yyyy} to {segTill:dd-MM-yyyy} - Qty {pendingQty}"
                    });
                }
            }

            return result
                .OrderBy(x => x.RequestNo)
                .ThenBy(x => x.FromDate)
                .ThenBy(x => x.TillDate)
                .ThenBy(x => x.OverrideId)
                .ToList();
        }

        public async Task<QrCodeRequestModel> AddUpdateQrWithImagesAsync(QrCodeRequestModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.CompanyId <= 0)
                throw new Exception("CompanyId must be greater than 0.");

            if (string.IsNullOrWhiteSpace(model.CompanyName))
                throw new Exception("CompanyName is required.");

            if (model.RequestId <= 0)
                throw new Exception("RequestId must be greater than 0.");

            if (model.NoofQR <= 0)
                throw new Exception("NoofQR must be greater than 0.");

            if (model.QRValidFrom == default)
                throw new Exception("QRValidFrom is required.");

            if (model.QRValidTill == default)
                throw new Exception("QRValidTill is required.");

            if (model.QRValidFrom.Date > model.QRValidTill.Date)
                throw new Exception("QRValidFrom cannot be greater than QRValidTill.");

            if (model.QrImages == null || !model.QrImages.Any())
                throw new Exception("At least one QR image is required.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                QrCodeRequest qrRequest;
                int? safeOverrideId = (model.OverrideId.HasValue && model.OverrideId.Value > 0)
                    ? model.OverrideId.Value
                    : null;

                if (model.Id > 0)
                {
                    qrRequest = await _context.QrCodeRequest
                        .FirstOrDefaultAsync(x => x.Id == model.Id && x.IsActive)
                        ?? throw new Exception("QR request not found.");

                    qrRequest.CompanyId = model.CompanyId;
                    qrRequest.CompanyName = model.CompanyName.Trim();
                    qrRequest.CompanyEmail = string.IsNullOrWhiteSpace(model.CompanyEmail)
                        ? null
                        : model.CompanyEmail.Trim();
                    qrRequest.RequestId = model.RequestId;
                    qrRequest.OverrideId = safeOverrideId;
                    qrRequest.NoofQR = model.NoofQR;
                    qrRequest.QRValidFrom = model.QRValidFrom;
                    qrRequest.QRValidTill = model.QRValidTill;
                    qrRequest.IsActive = model.IsActive;
                    qrRequest.UpdatedDate = DateTime.UtcNow;
                    qrRequest.UpdatedBy = model.UpdatedBy > 0 ? model.UpdatedBy : model.CreatedBy;

                    _context.QrCodeRequest.Update(qrRequest);

                    var oldImages = await _context.QrImage
                        .Where(x => x.Qrcoderequestid == qrRequest.Id && x.IsActive)
                        .ToListAsync();

                    foreach (var old in oldImages)
                    {
                        old.IsActive = false;
                        old.UpdatedDate = DateTime.UtcNow;
                        old.UpdatedBy = (model.UpdatedBy > 0 ? model.UpdatedBy : model.CreatedBy).ToString();
                    }
                }
                else
                {
                    qrRequest = new QrCodeRequest
                    {
                        CompanyId = model.CompanyId,
                        CompanyName = model.CompanyName.Trim(),
                        CompanyEmail = string.IsNullOrWhiteSpace(model.CompanyEmail)
                            ? null
                            : model.CompanyEmail.Trim(),
                        RequestId = model.RequestId,
                        OverrideId = safeOverrideId,
                        NoofQR = model.NoofQR,
                        QRValidFrom = model.QRValidFrom,
                        QRValidTill = model.QRValidTill,
                        IsActive = model.IsActive,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy,
                        UpdatedDate = null,
                        UpdatedBy = 0
                    };

                    _context.QrCodeRequest.Add(qrRequest);
                }

                await _context.SaveChangesAsync();

                foreach (var qr in model.QrImages)
                {
                    if (string.IsNullOrWhiteSpace(qr.QrCodeImageBase64))
                        throw new Exception("QR image Base64 is required for each QR.");

                    byte[] imageBytes;
                    try
                    {
                        imageBytes = Convert.FromBase64String(qr.QrCodeImageBase64);
                    }
                    catch
                    {
                        throw new Exception("Invalid QR image Base64 format.");
                    }

                    var qrImage = new QrImage
                    {
                        Qrcoderequestid = qrRequest.Id,
                        QrCodeText = qr.QrCodeText,
                        QrCodeImage = imageBytes,
                        IsActive = true,
                        SerialNo = qr.SerialNo,
                        UniqueCode = qr.UniqueCode,
                        IsUsed = false,
                        UsedDate = null,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = model.CreatedBy.ToString(),
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = (model.UpdatedBy > 0 ? model.UpdatedBy : model.CreatedBy).ToString()
                    };

                    _context.QrImage.Add(qrImage);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                model.Id = qrRequest.Id;
                model.OverrideId = safeOverrideId;

                return model;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<QrCodeRequestModel> SubmitQrApprovalRequestAsync(QrCodeRequestModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.CompanyId <= 0)
                throw new Exception("CompanyId must be greater than 0.");

            if (string.IsNullOrWhiteSpace(model.CompanyName))
                throw new Exception("CompanyName is required.");

            if (model.RequestId <= 0)
                throw new Exception("RequestId must be greater than 0.");

            if (model.NoofQR <= 0)
                throw new Exception("NoofQR must be greater than 0.");

            if (model.QRValidFrom == default)
                throw new Exception("QRValidFrom is required.");

            if (model.QRValidTill == default)
                throw new Exception("QRValidTill is required.");

            if (model.QRValidFrom.Date > model.QRValidTill.Date)
                throw new Exception("QRValidFrom cannot be greater than QRValidTill.");

            int? safeOverrideId = (model.OverrideId.HasValue && model.OverrideId.Value > 0)
                ? model.OverrideId.Value
                : null;

            var pendingItems = await GetQrPendingDropdown();

            var matchedPending = pendingItems.FirstOrDefault(x =>
                x.RequestId == model.RequestId &&
                (x.OverrideId ?? 0) == (safeOverrideId ?? 0) &&
                x.FromDate.Date == model.QRValidFrom.Date &&
                x.TillDate.Date == model.QRValidTill.Date);

            if (matchedPending == null)
                throw new Exception("Selected pending segment is not available. Please refresh and try again.");

            if (model.NoofQR > matchedPending.Qty)
                throw new Exception($"Only {matchedPending.Qty} QR(s) can be requested for this segment.");

            var alreadyPending = await _context.QrCodeRequest.AnyAsync(x =>
                x.IsActive &&
                x.ApprovalStatus == 0 &&
                x.RequestId == model.RequestId &&
                (x.OverrideId ?? 0) == (safeOverrideId ?? 0) &&
                x.QRValidFrom.Date == model.QRValidFrom.Date &&
                x.QRValidTill.Date == model.QRValidTill.Date);

            if (alreadyPending)
                throw new Exception("Approval request already pending for this segment.");

            var entity = new QrCodeRequest
            {
                CompanyId = model.CompanyId,
                CompanyName = model.CompanyName.Trim(),
                CompanyEmail = string.IsNullOrWhiteSpace(model.CompanyEmail) ? null : model.CompanyEmail.Trim(),
                RequestId = model.RequestId,
                OverrideId = safeOverrideId,
                NoofQR = model.NoofQR,
                QRValidFrom = model.QRValidFrom,
                QRValidTill = model.QRValidTill,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = null,
                CreatedBy = model.CreatedBy,
                UpdatedBy = 0,

                ApprovalStatus = 0,
                RequestedBy = model.CreatedBy,
                RequestedDate = DateTime.UtcNow,
                ApprovedBy = null,
                ApprovedDate = null,
                RejectedBy = null,
                RejectedDate = null,
                RejectionReason = null
            };

            _context.QrCodeRequest.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
            model.OverrideId = safeOverrideId;
            model.ApprovalStatus = entity.ApprovalStatus;
            model.RequestedBy = entity.RequestedBy;
            model.RequestedDate = entity.RequestedDate;

            return model;
        }

        public async Task<string> ApproveQrRequestAsync(int qrCodeRequestId, int approvedBy)
        {
            if (qrCodeRequestId <= 0)
                throw new Exception("Valid QR request id is required.");

            if (approvedBy <= 0)
                throw new Exception("Valid approvedBy is required.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var qrRequest = await _context.QrCodeRequest
                    .FirstOrDefaultAsync(x => x.Id == qrCodeRequestId && x.IsActive);

                if (qrRequest == null)
                    throw new Exception("QR request not found.");

                if (qrRequest.ApprovalStatus != 0)
                    throw new Exception("Only pending QR requests can be approved.");

                var existingApproved = await _context.QrCodeRequest.AnyAsync(x =>
                    x.Id != qrRequest.Id &&
                    x.IsActive &&
                    x.ApprovalStatus == 1 &&
                    x.RequestId == qrRequest.RequestId &&
                    (x.OverrideId ?? 0) == (qrRequest.OverrideId ?? 0) &&
                    x.QRValidFrom.Date == qrRequest.QRValidFrom.Date &&
                    x.QRValidTill.Date == qrRequest.QRValidTill.Date);

                if (existingApproved)
                    throw new Exception("This QR request is already approved for the selected segment.");

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
                    IsActive = qrRequest.IsActive,
                    CreatedBy = qrRequest.CreatedBy,
                    UpdatedBy = approvedBy
                };

                var qrResults = await GenerateUniqueQrs(generateModel);

                if (qrResults == null || !qrResults.Any())
                    throw new Exception("QR generation failed.");

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
                        throw new Exception("Generated QR image is empty.");
                    }

                    var qrImage = new QrImage
                    {
                        Qrcoderequestid = qrRequest.Id,
                        QrCodeText = qr.Text,
                        QrCodeImage = imageBytes,
                        IsActive = true,
                        SerialNo = qr.SerialNo,
                        UniqueCode = qr.UniqueCode,
                        IsUsed = false,
                        UsedDate = null,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = approvedBy.ToString(),
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = approvedBy.ToString()
                    };

                    _context.QrImage.Add(qrImage);
                }

                qrRequest.ApprovalStatus = 1;
                qrRequest.ApprovedBy = approvedBy;
                qrRequest.ApprovedDate = DateTime.UtcNow;
                qrRequest.UpdatedBy = approvedBy;
                qrRequest.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await SendQrEmailToCompanyAsync(qrRequest.Id);

                return "QR request approved successfully. QR generated and email sent.";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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
    }
}