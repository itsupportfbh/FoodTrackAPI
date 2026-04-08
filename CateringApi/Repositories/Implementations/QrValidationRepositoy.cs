using CateringApi.Data;
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
            var qrImage = GetQrImageByUniqueCode(UniqueCode);

            if (qrImage == null || qrImage.Id == 0)
                return Fail("Invalid QR code.");

            var request = await GetQrRequestByIdAsync(RequestId);

            if (request == null)
                return Fail("QR request not found.");

            var now = DateTime.Now;

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

            // 3. Check 1-hour cooldown
            if (qrImage.IsUsed && qrImage.UsedDate.HasValue)
            {
                var minutesSinceLastUse = (now - qrImage.UsedDate.Value).TotalMinutes;

             

                if (minutesSinceLastUse < 60)
                {
                    var waitMinutes = (int)Math.Ceiling(60 - minutesSinceLastUse);
                    return Fail($"Meal already collected. Please wait {waitMinutes} minute(s) before scanning again.");
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