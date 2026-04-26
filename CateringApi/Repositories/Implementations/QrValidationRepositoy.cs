using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Scanner;
using CateringApi.Models;
using CateringApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CateringApi.Repositories.Implementations
{
    public class QrValidationRepository : IQrValidationRepository
    {
        private readonly FoodDBContext _context;

        public QrValidationRepository(FoodDBContext context)
        {
            _context = context;
        }

        public QrImage? GetQrImageByUniqueCode(string uniqueCode)
        {
            var actualCode = ExtractUniqueCodeFromScannedText(uniqueCode);

            return _context.QrImage
                .FirstOrDefault(x => x.UniqueCode == actualCode && x.IsActive);
        }

        public async Task<RequestHeader?> GetQrRequestByIdAsync(int requestId)
        {
            return await _context.RequestHeader
                .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive);
        }

        public async Task MarkQrAsUsedAsync(int qrImageId, DateTime usedDate)
        {
            var qr = await _context.QrImage.FindAsync(qrImageId);
            if (qr == null) return;

            qr.IsUsed = true;
            qr.UsedDate = usedDate;
            qr.QRScannedCount = (qr.QRScannedCount ?? 0) + 1;

            await _context.SaveChangesAsync();
        }

        public async Task DeactivateRequestAndImagesAsync(int qrcoderequestid, string uniqueCode)
        {
            var actualCode = ExtractUniqueCodeFromScannedText(uniqueCode);

            var image = await _context.QrImage
                .FirstOrDefaultAsync(q =>
                    q.Qrcoderequestid == qrcoderequestid &&
                    q.UniqueCode == actualCode);

            if (image != null)
            {
                image.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        private static string NormalizePlanType(string? planType)
        {
            return string.IsNullOrWhiteSpace(planType) ? "Basic" : planType.Trim();
        }

        private static string ExtractUniqueCodeFromScannedText(string scannedText)
        {
            if (string.IsNullOrWhiteSpace(scannedText))
                return string.Empty;

            scannedText = scannedText.Trim();

            // Supports new QR text format:
            // PLAN:Premium|CODE:CSPLABC...
            if (scannedText.StartsWith("PLAN:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = scannedText.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var codePart = parts.FirstOrDefault(x =>
                    x.StartsWith("CODE:", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(codePart))
                    return codePart.Substring(5).Trim();
            }

            // Old format - directly unique code
            return scannedText;
        }

        private static string? ExtractPlanTypeFromScannedText(string scannedText)
        {
            if (string.IsNullOrWhiteSpace(scannedText))
                return null;

            scannedText = scannedText.Trim();

            if (!scannedText.StartsWith("PLAN:", StringComparison.OrdinalIgnoreCase))
                return null;

            var parts = scannedText.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var planPart = parts.FirstOrDefault(x =>
                x.StartsWith("PLAN:", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(planPart))
                return null;

            return planPart.Substring(5).Trim();
        }

        private async Task AddScanLogAsync(
            int qrImageId,
            int qrCodeRequestId,
            int requestId,
            int sessionId,
            string uniqueCode,
            bool isAllowed,
            string message,
            int? createdBy = null)
        {
            var log = new QrScanLog
            {
                QrImageId = qrImageId,
                QrCodeRequestId = qrCodeRequestId,
                RequestId = requestId,
                SessionId = sessionId,
                ScanDate = DateTime.Today,
                ScanDateTime = DateTime.Now,
                UniqueCode = uniqueCode,
                IsAllowed = isAllowed,
                Message = message,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            _context.QrScanLog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<QrValidationResult> ValidateScanAsync(string uniqueCode)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var nowTime = new TimeSpan(now.Hour, now.Minute, 0);

            if (string.IsNullOrWhiteSpace(uniqueCode))
                return Fail("QR code is required.");

            string actualCode = ExtractUniqueCodeFromScannedText(uniqueCode);

            if (string.IsNullOrWhiteSpace(actualCode))
                return Fail("Invalid QR code.");

            string finalPlanType = "";

            if (actualCode.Contains("-STD-", StringComparison.OrdinalIgnoreCase))
                finalPlanType = "Standard";
            else if (actualCode.Contains("-BSC-", StringComparison.OrdinalIgnoreCase))
                finalPlanType = "Basic";
            else if (actualCode.Contains("-PRM-", StringComparison.OrdinalIgnoreCase))
                finalPlanType = "Premium";

            if (string.IsNullOrWhiteSpace(finalPlanType))
                return Fail("QR plan type is missing.");

            var qrDataList = await (
                from qi in _context.QrImage
                join qcr in _context.QrCodeRequest on qi.Qrcoderequestid equals qcr.Id
                where qi.UniqueCode == actualCode
                      && qi.IsActive
                      && qcr.IsActive
                select new
                {
                    QrImage = qi,
                    QrCodeRequest = qcr
                }
            ).ToListAsync();

            var matchedQr = qrDataList.FirstOrDefault(x =>
                NormalizePlanType(x.QrCodeRequest.PlanType ?? "") == finalPlanType);

            if (matchedQr == null)
                return Fail($"Invalid {finalPlanType} QR code.");

            var qrImage = matchedQr.QrImage;
            var qrCodeRequest = matchedQr.QrCodeRequest;

            if (qrCodeRequest.ApprovalStatus != 1)
                return Fail("QR request is not approved.");

            if (qrCodeRequest.QRValidFrom.Date > qrCodeRequest.QRValidTill.Date)
                return Fail("Invalid QR validity range. Valid From cannot be greater than Valid Till.");

            if (today < qrCodeRequest.QRValidFrom.Date)
                return Fail($"QR is not yet valid. Valid from {qrCodeRequest.QRValidFrom:dd-MM-yyyy}.");

            if (today > qrCodeRequest.QRValidTill.Date)
                return Fail("QR validity period has ended. Access denied.");

            var requestHeader = await _context.RequestHeader
                .Where(x =>
                    x.Id == qrCodeRequest.RequestId &&
                    x.CompanyId == qrCodeRequest.CompanyId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyId
                })
                .FirstOrDefaultAsync();

            if (requestHeader == null)
                return Fail($"Request not found. RequestId={qrCodeRequest.RequestId}, CompanyId={qrCodeRequest.CompanyId}");

            string qrRequestPlanType = NormalizePlanType(qrCodeRequest.PlanType ?? "");

            var assignment = await _context.QrUserAssignment
                .FirstOrDefaultAsync(x =>
                    x.QrCodeRequestId == qrCodeRequest.Id &&
                    x.QrImageId == qrImage.Id &&
                    x.UniqueCode == actualCode &&
                    x.CompanyId == qrCodeRequest.CompanyId &&
                    x.RequestId == qrCodeRequest.RequestId &&
                    (x.OverrideId ?? 0) == (qrCodeRequest.OverrideId ?? 0) &&
                    x.IsActive);

            if (assignment == null)
                return Fail("This QR is not assigned to a valid user.");

            var assignedUser = await _context.UserMaster
                .FirstOrDefaultAsync(x =>
                    x.Id == assignment.UserId &&
                    x.CompanyId == assignment.CompanyId &&
                    x.IsActive &&
                    !x.IsDelete &&
                    !string.IsNullOrWhiteSpace(x.Email));

            if (assignedUser == null)
                return Fail("Assigned user is not active.");

            string userPlanType = NormalizePlanType(assignedUser.PlanType ?? "");

            if (!string.Equals(userPlanType, qrRequestPlanType, StringComparison.OrdinalIgnoreCase))
                return Fail($"User plan type mismatch. Expected {qrRequestPlanType}, user plan is {userPlanType}.");

            var sessionList = await (
                from csm in _context.CompanySessionMapping
                join s in _context.Session on csm.SessionId equals s.Id
                where csm.CompanyId == qrCodeRequest.CompanyId
                      && csm.IsActive
                      && s.IsActive
                select new
                {
                    csm.CompanyId,
                    SessionId = s.Id,
                    s.SessionName,
                    csm.FromTime,
                    csm.ToTime
                }
            ).ToListAsync();

            if (sessionList.Count == 0)
                return Fail("No session timing configured for this company.");

            var matchingSession = sessionList.FirstOrDefault(s =>
                (s.FromTime <= s.ToTime && nowTime >= s.FromTime && nowTime <= s.ToTime) ||
                (s.FromTime > s.ToTime && (nowTime >= s.FromTime || nowTime <= s.ToTime))
            );

            if (matchingSession == null)
                return Fail("Scanning not allowed at this time for the company's session timing.");

            int sessionId = matchingSession.SessionId;

            var mealRequests = await _context.MealRequest
                .Where(x =>
                    x.CompanyId == qrCodeRequest.CompanyId &&
                    x.UserId == assignment.UserId &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today)
                .ToListAsync();

            if (!mealRequests.Any())
                return Fail("No meal request found for this user today.");

            var mealLocationIds = mealRequests
                .Select(x => x.LocationId)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!mealLocationIds.Any())
                return Fail("Meal request location is missing.");

            var alreadyScannedThisSession = await _context.QrScanLog.AnyAsync(x =>
                x.QrImageId == qrImage.Id &&
                x.QrCodeRequestId == qrCodeRequest.Id &&
                x.RequestId == qrCodeRequest.RequestId &&
                x.SessionId == sessionId &&
                x.ScanDate == today &&
                x.IsAllowed);

            if (alreadyScannedThisSession)
                return Fail($"Meal already collected for {matchingSession.SessionName}.");

            int requiredQty = 0;

            if (qrCodeRequest.OverrideId.HasValue && qrCodeRequest.OverrideId.Value > 0)
            {
                decimal overrideQty = await _context.RequestOverrideDetail
     .Where(x =>
         x.RequestOverrideId == qrCodeRequest.OverrideId.Value &&
         x.IsActive &&
         !x.IsCancelled &&
         x.PlanType != null &&
         x.PlanType.Trim().ToLower() == qrRequestPlanType.ToLower())
     .SumAsync(x => (decimal?)x.OverrideQty) ?? 0;

                requiredQty = Convert.ToInt32(overrideQty);
            }
            else
            {
                decimal baseQty = await _context.RequestDetail
    .Where(x =>
        x.RequestHeaderId == qrCodeRequest.RequestId &&
        x.IsActive &&
        x.PlanType != null &&
        x.PlanType.Trim().ToLower() == qrRequestPlanType.ToLower())
    .SumAsync(x => (decimal?)x.Qty) ?? 0;

                requiredQty = Convert.ToInt32(baseQty);
            }

            if (requiredQty <= 0)
                return Fail($"No quantity configured for {matchingSession.SessionName} and plan {qrRequestPlanType}.");

            int usedQtyToday = await _context.QrScanLog
                .Where(x =>
                    x.RequestId == qrCodeRequest.RequestId &&
                    x.SessionId == sessionId &&
                    x.ScanDate == today &&
                    x.IsAllowed)
                .CountAsync();

            if (usedQtyToday >= requiredQty)
                return Fail($"{matchingSession.SessionName} quantity limit reached for today.");

            qrImage.IsUsed = true;
            qrImage.UsedDate = now;
            qrImage.QRScannedCount = (qrImage.QRScannedCount ?? 0) + 1;

            var log = new QrScanLog
            {
                QrImageId = qrImage.Id,
                QrCodeRequestId = qrCodeRequest.Id,
                RequestId = qrCodeRequest.RequestId,
                SessionId = sessionId,
                ScanDate = today,
                ScanDateTime = now,
                UniqueCode = actualCode,
                IsAllowed = true,
                Message = "Access granted",
                CreatedDate = now,
                CreatedBy = assignment.UserId
            };

            _context.QrScanLog.Add(log);
            await _context.SaveChangesAsync();

            return new QrValidationResult
            {
                IsAllowed = true,
                Message = $"Access granted for {matchingSession.SessionName}. Plan: {qrRequestPlanType}. Enjoy your meal!",
                SessionId = sessionId,
                SessionName = matchingSession.SessionName,
                ScanTime = now
            };
        }
        private static QrValidationResult Fail(string message)
        {
            return new QrValidationResult
            {
                IsAllowed = false,
                Message = message,
                SessionId = null,
                SessionName = null,
                ScanTime = DateTime.Now
            };
        }
    }
}