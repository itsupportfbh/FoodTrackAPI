using CateringApi.Data;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Dashboard;
using CateringApi.DTOs.Session;
using CateringApi.DTOs.QR;
using CateringApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CateringApi.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly FoodDBContext _context;

        public DashboardRepository(FoodDBContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> GetDashboardData()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            // -----------------------------
            // Top cards
            // -----------------------------
            var totalCompanies = await _context.CompanyMaster
                .Where(x => x.IsActive)
                .CountAsync();

            var totalOrders = await _context.RequestHeader
                .Where(x => x.IsActive)
                .CountAsync();

            var totalQrCodes = await _context.QrCodeRequest
                .Where(x => x.IsActive)
                .SumAsync(x => (int?)x.NoofQR) ?? 0;

            // -----------------------------
            // Session wise total ordered qty
            // -----------------------------
            var ordersBySession = await (
                from rd in _context.RequestDetail
                join rh in _context.RequestHeader on rd.RequestHeaderId equals rh.Id
                join s in _context.Session on rd.SessionId equals s.Id
                where rd.IsActive && rh.IsActive && s.IsActive
                group rd by new { rd.SessionId, s.SessionName } into g
                select new SessionOrderDTO
                {
                    SessionName = g.Key.SessionName ?? string.Empty,
                    TotalQty = g.Sum(x => (decimal?)x.Qty) ?? 0
                }
            ).ToListAsync();

            // -----------------------------
            // Company wise total ordered qty
            // -----------------------------
            var companyWiseOrders = await (
                from rd in _context.RequestDetail
                join rh in _context.RequestHeader on rd.RequestHeaderId equals rh.Id
                join c in _context.CompanyMaster on rh.CompanyId equals c.Id
                where rd.IsActive && rh.IsActive && c.IsActive
                group rd by new { c.Id, c.CompanyName } into g
                select new CompanyOrderDTO
                {
                    CompanyId = g.Key.Id,
                    CompanyName = g.Key.CompanyName ?? string.Empty,
                    TotalQty = g.Sum(x => (decimal?)x.Qty) ?? 0,
                    RedeemQty = 0,
                    PendingQty = 0
                }
            )
            .OrderByDescending(x => x.TotalQty)
            .Take(8)
            .ToListAsync();

            // -----------------------------
            // Today / Yesterday scans
            // -----------------------------
            var todayCount = await _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == today)
                .CountAsync();

            var yesterdayCount = await _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == yesterday)
                .CountAsync();

            // -----------------------------
            // Latest scanned QR list
            // -----------------------------
            var latestUsedQRs = await (
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                join c in _context.CompanyMaster on qr.CompanyId equals c.Id
                join s in _context.Session on log.SessionId equals s.Id
                where log.IsAllowed
                orderby log.ScanDateTime descending
                select new LatestQrDTO
                {
                    UniqueCode = log.UniqueCode ?? string.Empty,
                    CompanyName = c.CompanyName ?? string.Empty,
                    UsedDate = log.ScanDateTime,
                    SessionName = s.SessionName ?? string.Empty
                }
            )
            .Take(4)
            .ToListAsync();

            // -----------------------------
            // Company wise redeemed qty
            // -----------------------------
            var redeemMap = await (
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                where log.IsAllowed
                group log by qr.CompanyId into g
                select new
                {
                    CompanyId = g.Key,
                    RedeemQty = g.Count()
                }
            ).ToDictionaryAsync(x => x.CompanyId, x => x.RedeemQty);

            foreach (var item in companyWiseOrders)
            {
                item.RedeemQty = redeemMap.ContainsKey(item.CompanyId)
                    ? redeemMap[item.CompanyId]
                    : 0;

                item.PendingQty = item.TotalQty - item.RedeemQty;

                if (item.PendingQty < 0)
                    item.PendingQty = 0;
            }

            // -----------------------------
            // Summary totals
            // -----------------------------
            var totalOrderedQty = companyWiseOrders.Sum(x => x.TotalQty);
            var totalRedeemedQty = companyWiseOrders.Sum(x => x.RedeemQty);
            var totalPendingQty = companyWiseOrders.Sum(x => x.PendingQty);

            return new DashboardDTO
            {
                TotalCompanies = totalCompanies,
                TotalOrders = totalOrders,
                TotalQRCodes = totalQrCodes,
                TodayScans = todayCount,
                YesterdayScans = yesterdayCount,
                TotalOrderedQty = totalOrderedQty,
                TotalRedeemedQty = totalRedeemedQty,
                TotalPendingQty = totalPendingQty,
                TotalOrdersBySession = ordersBySession,
                TotalcompanyWiseOrders = companyWiseOrders,
                TotallatestUsedQRs = latestUsedQRs
            };
        }

        private class DashboardEffectiveRow
        {
            public int SessionId { get; set; }
            public int CompanyId { get; set; }
            public decimal Qty { get; set; }
        }
    }
}
