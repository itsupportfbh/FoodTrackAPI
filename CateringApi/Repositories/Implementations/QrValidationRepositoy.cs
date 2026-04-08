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

        public async Task<QrValidationResult> ValidateScanAsync(string UniqueCode, int RequestId, int CompanyId)
        {

            var now = DateTime.Now;

            var qrImage = GetQrImageByUniqueCode(UniqueCode);

            if (qrImage == null || qrImage.Id == 0)
                return Fail("Invalid QR code.");

            var request = await GetQrRequestByIdAsync(RequestId);

            if (request == null)
                return Fail("QR request not found.");


            // 1. Check request date range
            if (now < request.FromDate)
                return Fail($"QR is not yet valid. Valid from {request.FromDate:dd-MM-yyyy}.");

            if (now > request.ToDate)
            {
                await DeactivateRequestAndImagesAsync(qrImage.Qrcoderequestid, UniqueCode);
                return Fail("QR validity period has ended. Access denied.");
            }

            // 2. Check if this specific QR is still active
            if (!qrImage.IsActive)
                return Fail("This QR code is no longer active.");

            // 3. Check session time window for this request
            var sessionList = await (from ss in _context.Session
                                      join rd in _context.RequestDetail on ss.Id equals rd.SessionId
                                      where rd.RequestHeaderId == RequestId && ss.IsActive && rd.IsActive
                                      select new
                                      {
                                          ss.Id,
                                          ss.SessionName,
                                          ss.FromTime,
                                          ss.ToTime
                                      }).ToListAsync();

            // Determine which sessions currently apply (compare only hours and minutes, ignore seconds)
            var nowTime = now.TimeOfDay;
            var nowTimeTrimmed = new TimeSpan(nowTime.Hours, nowTime.Minutes, 0);

            var matchingSessions = sessionList.Where(s => s.FromTime.HasValue && s.ToTime.HasValue &&
            (
                // normal range: From <= To
                (new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0) <= new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0)
                    && nowTimeTrimmed >= new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0)
                    && nowTimeTrimmed <= new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0))
                // overnight range: From > To (e.g., 23:00 - 02:00)
                || (new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0) > new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0)
                    && (nowTimeTrimmed >= new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0)
                        || nowTimeTrimmed <= new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0))))
            ).ToList();

            if (!matchingSessions.Any())
                return Fail("Scanning not allowed at this time for the request's session.");

            // If scanning is within a session window, ensure this QR hasn't already been used for the same session
            if (qrImage.IsUsed && qrImage.UsedDate.HasValue)
            {
                var usedTime = qrImage.UsedDate.Value.TimeOfDay;
                var usedTimeTrimmed = new TimeSpan(usedTime.Hours, usedTime.Minutes, 0);

                foreach (var s in matchingSessions)
                {
                    if (!s.FromTime.HasValue || !s.ToTime.HasValue)
                        continue;

                    var fromTrimmed = new TimeSpan(s.FromTime.Value.Hours, s.FromTime.Value.Minutes, 0);
                    var toTrimmed = new TimeSpan(s.ToTime.Value.Hours, s.ToTime.Value.Minutes, 0);

                    // normal range: From <= To
                    if (fromTrimmed <= toTrimmed)
                    {
                        if (usedTimeTrimmed >= fromTrimmed && usedTimeTrimmed <= toTrimmed)
                            return Fail("Meal already collected for this session.");
                    }
                    else
                    {
                        // overnight range: From > To (e.g., 23:00 - 02:00)
                        if (usedTimeTrimmed >= fromTrimmed || usedTimeTrimmed <= toTrimmed)
                            return Fail("Meal already collected for this session.");
                    }
                }
            }

            // 4. All checks passed — mark as used
            await MarkQrAsUsedAsync(qrImage.Id, now);

            return new QrValidationResult
            {
                IsAllowed = true,
                Message = "Access granted. Enjoy your meal!"
            };
        }

        private static QrValidationResult Fail(string message) =>
            new QrValidationResult { IsAllowed = false, Message = message };
    }
}