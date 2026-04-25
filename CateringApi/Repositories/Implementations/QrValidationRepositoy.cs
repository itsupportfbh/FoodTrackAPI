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
            string? scannedPlanType = ExtractPlanTypeFromScannedText(uniqueCode);

            if (string.IsNullOrWhiteSpace(actualCode))
                return Fail("Invalid QR code.");

            var qrImage = await _context.QrImage
                .FirstOrDefaultAsync(x => x.UniqueCode == actualCode && x.IsActive);

            if (qrImage == null)
                return Fail("Invalid QR code.");

            var qrCodeRequest = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == qrImage.Qrcoderequestid && x.IsActive);

            if (qrCodeRequest == null)
                return Fail("QR request not found.");

            var requestHeader = await _context.RequestHeader
                .FirstOrDefaultAsync(x => x.Id == qrCodeRequest.RequestId && x.IsActive);

            if (requestHeader == null)
                return Fail("Request not found.");

            // PlanType validation
            string requestPlanType = NormalizePlanType(requestHeader.PlanType);
            string qrRequestPlanType = NormalizePlanType(qrCodeRequest.PlanType);
            string finalQrPlanType = !string.IsNullOrWhiteSpace(scannedPlanType)
                ? NormalizePlanType(scannedPlanType)
                : qrRequestPlanType;

            if (!string.Equals(requestPlanType, qrRequestPlanType, StringComparison.OrdinalIgnoreCase))
                return Fail($"Plan type mismatch. Request plan is {requestPlanType}, QR request plan is {qrRequestPlanType}.");

            if (!string.Equals(requestPlanType, finalQrPlanType, StringComparison.OrdinalIgnoreCase))
                return Fail($"Scanned QR plan type does not match request plan. Expected {requestPlanType}, scanned {finalQrPlanType}.");

            // QR batch validity only
            if (today < qrCodeRequest.QRValidFrom.Date)
                return Fail($"QR is not yet valid. Valid from {qrCodeRequest.QRValidFrom:dd-MM-yyyy}.");

            if (today > qrCodeRequest.QRValidTill.Date)
                return Fail("QR validity period has ended. Access denied.");

            // Company-specific session timing
            var sessionList = await (
                from rd in _context.RequestDetail
                join s in _context.Session on rd.SessionId equals s.Id
                join csm in _context.CompanySessionMapping
                    on new { SessionId = rd.SessionId, CompanyId = requestHeader.CompanyId }
                    equals new { SessionId = csm.SessionId, CompanyId = csm.CompanyId }
                where rd.RequestHeaderId == qrCodeRequest.RequestId
                      && rd.IsActive
                      && s.IsActive
                      && csm.IsActive
                select new CompanySessionMappingDto
                {
                    CompanyId = csm.CompanyId,
                    SessionId = s.Id,
                    SessionName = s.SessionName,
                    FromTime = csm.FromTime,
                    ToTime = csm.ToTime,
                    IsActive = csm.IsActive
                }
            )
            .Distinct()
            .ToListAsync();

            if (sessionList == null || sessionList.Count == 0)
                return Fail("No session timing configured for this company.");

            var matchingSession = sessionList.FirstOrDefault(s =>
                (s.FromTime <= s.ToTime && nowTime >= s.FromTime && nowTime <= s.ToTime) ||
                (s.FromTime > s.ToTime && (nowTime >= s.FromTime || nowTime <= s.ToTime))
            );

            if (matchingSession == null)
                return Fail("Scanning not allowed at this time for the company's session timing.");

            // Same QR + same day + same session => deny
            var alreadyScannedThisSession = await _context.QrScanLog.AnyAsync(x =>
                x.QrImageId == qrImage.Id &&
                x.RequestId == qrCodeRequest.RequestId &&
                x.SessionId == matchingSession.SessionId &&
                x.ScanDate == today &&
                x.IsAllowed);

            if (alreadyScannedThisSession)
                return Fail($"Meal already collected for {matchingSession.SessionName}.");

            // Find active override for today
            var activeOverride = await _context.RequestOverride
                .Where(x =>
                    x.RequestHeaderId == qrCodeRequest.RequestId &&
                    x.IsActive &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int requiredQty = 0;

            if (activeOverride != null)
            {
                requiredQty = await _context.RequestOverrideDetail
                    .Where(x =>
                        x.RequestOverrideId == activeOverride.Id &&
                        x.SessionId == matchingSession.SessionId &&
                        x.IsActive &&
                        !x.IsCancelled)
                    .SumAsync(x => (int?)x.OverrideQty) ?? 0;
            }
            else
            {
                requiredQty = await _context.RequestDetail
                    .Where(x =>
                        x.RequestHeaderId == qrCodeRequest.RequestId &&
                        x.SessionId == matchingSession.SessionId &&
                        x.IsActive)
                    .SumAsync(x => (int?)x.Qty) ?? 0;
            }

            if (requiredQty <= 0)
                return Fail($"No quantity configured for {matchingSession.SessionName}.");

            int usedQtyToday = await _context.QrScanLog
                .Where(x =>
                    x.RequestId == qrCodeRequest.RequestId &&
                    x.SessionId == matchingSession.SessionId &&
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
                SessionId = matchingSession.SessionId,
                ScanDate = today,
                ScanDateTime = now,
                UniqueCode = actualCode,
                IsAllowed = true,
                Message = "Access granted",
                CreatedDate = now,
                CreatedBy = null
            };

            _context.QrScanLog.Add(log);

            await _context.SaveChangesAsync();

            return new QrValidationResult
            {
                IsAllowed = true,
                Message = $"Access granted for {matchingSession.SessionName}. Plan: {requestPlanType}. Enjoy your meal!",
                SessionId = matchingSession.SessionId,
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