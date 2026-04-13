using CateringApi.Data;
using CateringApi.DTOModel;
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
            return _context.QrImage
                .FirstOrDefault(x => x.UniqueCode == uniqueCode && x.IsActive == true);
        }

        public async Task<RequestHeader?> GetQrRequestByIdAsync(int requestId)
        {
            return await _context.RequestHeader
                .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive == true);
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

        public async Task DeactivateRequestAndImagesAsync(int Qrcoderequestid, string UniqueCode)
        {
            var images = await _context.QrImage
                .Where(q => q.Qrcoderequestid == Qrcoderequestid && q.UniqueCode == UniqueCode).FirstOrDefaultAsync();

            if (images != null)
            {
                images.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        private static TimeSpan TrimToMinute(TimeSpan value)
        {
            return new TimeSpan(value.Hours, value.Minutes, 0);
        }

        private static bool IsWithinSession(TimeSpan now, TimeSpan from, TimeSpan to)
        {
            // Normal session: 08:00 to 10:00
            if (from <= to)
                return now >= from && now <= to;

            // Overnight session: 23:00 to 02:00
            return now >= from || now <= to;
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

            var qrImage = await _context.QrImage
                .FirstOrDefaultAsync(x => x.UniqueCode == uniqueCode && x.IsActive);

            if (qrImage == null)
                return Fail("Invalid QR code.");

            var qrCodeRequest = await _context.QrCodeRequest
                .FirstOrDefaultAsync(x => x.Id == qrImage.Qrcoderequestid && x.IsActive);

            if (qrCodeRequest == null)
                return Fail("QR request not found.");

            // IMPORTANT:
            // Historical OverrideId check panna koodadhu.
            // QR batch own validity dhaan source of truth.
            if (today < qrCodeRequest.QRValidFrom.Date)
                return Fail($"QR is not yet valid. Valid from {qrCodeRequest.QRValidFrom:dd-MM-yyyy}.");

            if (today > qrCodeRequest.QRValidTill.Date)
                return Fail("QR validity period has ended. Access denied.");

            var requestHeader = await _context.RequestHeader
                .FirstOrDefaultAsync(x => x.Id == qrCodeRequest.RequestId && x.IsActive);

            if (requestHeader == null)
                return Fail("Request not found.");

            // Find today's matching session from request details
            var sessionList = await (
                from ss in _context.Session
                join rd in _context.RequestDetail on ss.Id equals rd.SessionId
                where rd.RequestHeaderId == qrCodeRequest.RequestId
                      && ss.IsActive
                      && rd.IsActive
                select new
                {
                    ss.Id,
                    ss.SessionName,
                    ss.FromTime,
                    ss.ToTime
                }
            )
            .Distinct()
            .ToListAsync();

            var matchingSession = sessionList
                .Where(s => s.FromTime.HasValue && s.ToTime.HasValue)
                .Select(s => new
                {
                    s.Id,
                    s.SessionName,
                    From = new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0),
                    To = new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0)
                })
                .FirstOrDefault(s =>
                    // normal session
                    (s.From <= s.To && nowTime >= s.From && nowTime <= s.To)
                    ||
                    // overnight session
                    (s.From > s.To && (nowTime >= s.From || nowTime <= s.To))
                );

            if (matchingSession == null)
                return Fail("Scanning not allowed at this time for the request's session.");

            // SAME QR + SAME DAY + SAME SESSION => deny
            var alreadyScannedThisSession = await _context.QrScanLog.AnyAsync(x =>
                x.QrImageId == qrImage.Id &&
                x.RequestId == qrCodeRequest.RequestId &&
                x.SessionId == matchingSession.Id &&
                x.ScanDate == today &&
                x.IsAllowed);

            if (alreadyScannedThisSession)
                return Fail($"Meal already collected for {matchingSession.SessionName}.");

            // Find current active override for TODAY
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
                // Override session qty
                requiredQty = await _context.RequestOverrideDetail
                    .Where(x =>
                        x.RequestOverrideId == activeOverride.Id &&
                        x.SessionId == matchingSession.Id &&
                        x.IsActive &&
                        !x.IsCancelled)
                    .SumAsync(x => (int?)x.OverrideQty) ?? 0;
            }
            else
            {
                // Base request session qty
                requiredQty = await _context.RequestDetail
                    .Where(x =>
                        x.RequestHeaderId == qrCodeRequest.RequestId &&
                        x.SessionId == matchingSession.Id &&
                        x.IsActive)
                    .SumAsync(x => (int?)x.Qty) ?? 0;
            }

            if (requiredQty <= 0)
                return Fail($"No quantity configured for {matchingSession.SessionName}.");

            // Today total successful scans for this request + session
            int usedQtyToday = await _context.QrScanLog
                .Where(x =>
                    x.RequestId == qrCodeRequest.RequestId &&
                    x.SessionId == matchingSession.Id &&
                    x.ScanDate == today &&
                    x.IsAllowed)
                .CountAsync();

            if (usedQtyToday >= requiredQty)
                return Fail($"{matchingSession.SessionName} quantity limit reached for today.");

            // Mark last-use info only
            qrImage.IsUsed = true;
            qrImage.UsedDate = now;
            qrImage.QRScannedCount = (qrImage.QRScannedCount ?? 0) + 1;

            await _context.SaveChangesAsync();

            // Insert scan log
            var log = new QrScanLog
            {
                QrImageId = qrImage.Id,
                QrCodeRequestId = qrCodeRequest.Id,
                RequestId = qrCodeRequest.RequestId,
                SessionId = matchingSession.Id,
                ScanDate = today,
                ScanDateTime = now,
                UniqueCode = uniqueCode,
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
                Message = $"Access granted for {matchingSession.SessionName}. Enjoy your meal!",
                SessionId = matchingSession.Id,
                SessionName = matchingSession.SessionName,
                ScanTime = now
            };
        }

        private static QrValidationResult Fail(string message)
        {
            return new QrValidationResult
            {
                IsAllowed = false,
                Message = message
            };
        }

       

       
    }
}