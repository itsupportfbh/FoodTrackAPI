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
    .Take(4)
    .ToListAsync();

            var latestUsedQRs = await _context.QrImage
    .Where(q => q.IsUsed)
    .Join(_context.QrCodeRequest,
          qi => qi.Id,
          qr => qr.Id,
          (qi, qr) => new { qi, qr })
    .Join(_context.CompanyMaster,
          temp => temp.qr.CompanyId,
          c => c.Id,
          (temp, c) => new LatestQrDTO
          {
              CompanyName = c.CompanyName,
              UniqueCode = temp.qi.UniqueCode,
              UsedDate = temp.qi.UsedDate
          })
    .OrderByDescending(x => x.UsedDate)
    .Take(4)
    .ToListAsync();

            return new DashboardDTO
            {
                TotalCompanies = await _context.CompanyMaster.CountAsync(),
                TotalOrders = await _context.RequestHeader.CountAsync(),
                TotalQRCodes = await _context.QrCodeRequest.SumAsync(q => q.NoofQR),
                TotalOrdersBySession = ordersBySession,
                TotalcompanyWiseOrders = companyWiseOrders,
                TotallatestUsedQRs = latestUsedQRs
            };
        }
    }
}
