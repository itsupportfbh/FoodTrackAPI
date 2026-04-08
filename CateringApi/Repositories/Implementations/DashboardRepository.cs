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

            var ordersBySession = await _context.RequestDetail
            .Where(r => r.IsActive)
            .GroupBy(r => r.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                TotalQty = g.Sum(r => r.Qty)
            })
            .Join(_context.Session,
                  grp => grp.SessionId,
                  s => s.Id,
                  (grp, s) => new SessionOrderDTO
                  {
                      SessionName = s.SessionName,
                      TotalQty = grp.TotalQty
                  })
            .ToListAsync();

            var companyWiseOrders = await _context.RequestDetail
 .Where(rd => rd.IsActive)
 .Join(_context.RequestHeader,
       rd => rd.RequestHeaderId,
       rh => rh.Id,
       (rd, rh) => new { rd, rh })
 .Join(_context.CompanyMaster,
       temp => temp.rh.CompanyId,
       c => c.Id,
       (temp, c) => new { temp.rd, c })
 .GroupBy(x => new { x.c.Id, x.c.CompanyName })
 .Select(g => new CompanyOrderDTO
 {
     CompanyId = g.Key.Id,
     CompanyName = g.Key.CompanyName,
     TotalQty = g.Sum(x => x.rd.Qty)
 })
 .OrderByDescending(x => x.TotalQty)
 .Take(8)
 .ToListAsync();

            var latestUsedQRs = await (
      from qi in _context.QrImage
      join qr in _context.QrCodeRequest
          on qi.Qrcoderequestid equals qr.Id
      join c in _context.CompanyMaster
          on qr.CompanyId equals c.Id
      join rd in _context.RequestDetail
          on qr.RequestId equals rd.RequestHeaderId
      join s in _context.Session
          on rd.SessionId equals s.Id
      where qi.IsUsed
      group new { qi, c, s } by new
      {
          qi.UniqueCode,
          qi.UsedDate,
          c.CompanyName
      } into g
      select new LatestQrDTO
      {
          UniqueCode = g.Key.UniqueCode,
          CompanyName = g.Key.CompanyName,
          UsedDate = g.Key.UsedDate,
          SessionName = g.Select(x => x.s.SessionName).FirstOrDefault()
      }
  )
  .OrderByDescending(x => x.UsedDate)
  .Take(4)
  .ToListAsync();
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var todayCount = await _context.QrImage
                .CountAsync(x => x.IsUsed && x.UsedDate.Value.Date == today);

            var yesterdayCount = await _context.QrImage
                .CountAsync(x => x.IsUsed && x.UsedDate.Value.Date == yesterday);
            return new DashboardDTO
            {
                TotalCompanies = await _context.CompanyMaster.CountAsync(),
                TotalOrders = await _context.RequestHeader.CountAsync(),
                TotalQRCodes = await _context.QrCodeRequest.SumAsync(q => q.NoofQR),
                TotalOrdersBySession = ordersBySession,
                TotalcompanyWiseOrders = companyWiseOrders,
                TodayScans = todayCount,
                YesterdayScans = yesterdayCount,
                TotallatestUsedQRs = latestUsedQRs,
            };
        }
    }
}
