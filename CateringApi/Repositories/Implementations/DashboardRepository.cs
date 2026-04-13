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

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var totalCompanies = await _context.CompanyMaster
                .Where(x => x.IsActive)
                .CountAsync();

            var totalOrders = await _context.RequestHeader
                .Where(x => x.IsActive)
                .CountAsync();

            var totalQrCodes = await _context.QrCodeRequest
                .Where(x => x.IsActive)
                .SumAsync(x => (int?)x.NoofQR) ?? 0;

            var monthRequests = await _context.RequestHeader
                .Where(r => r.IsActive &&
                            r.FromDate <= monthEnd &&
                            r.ToDate >= monthStart)
                .Select(r => new
                {
                    r.Id,
                    r.CompanyId,
                    r.FromDate,
                    r.ToDate
                })
                .ToListAsync();

            var requestIds = monthRequests.Select(x => x.Id).ToList();

            var monthOverrides = await _context.RequestOverride
                .Where(o => o.IsActive &&
                            requestIds.Contains(o.RequestHeaderId) &&
                            o.FromDate <= monthEnd &&
                            o.ToDate >= monthStart)
                .Select(o => new
                {
                    o.Id,
                    o.RequestHeaderId,
                    o.FromDate,
                    o.ToDate,
                    o.CreatedDate,
                    o.UpdatedDate
                })
                .ToListAsync();

            var overrideIds = monthOverrides.Select(x => x.Id).ToList();

            var baseDetails = await _context.RequestDetail
                .Where(x => x.IsActive && requestIds.Contains(x.RequestHeaderId))
                .Select(x => new
                {
                    RequestDetailId = x.Id,
                    x.RequestHeaderId,
                    x.SessionId,
                    x.CuisineId,
                    x.LocationId,
                    Qty = (decimal?)x.Qty ?? 0
                })
                .ToListAsync();

            var overrideDetails = await _context.RequestOverrideDetail
                .Where(x => x.IsActive &&
                            overrideIds.Contains(x.RequestOverrideId) &&
                            !x.IsCancelled)
                .Select(x => new
                {
                    x.RequestOverrideId,
                    x.RequestDetailId,
                    x.SessionId,
                    x.CuisineId,
                    x.LocationId,
                    Qty = (decimal?)x.OverrideQty ?? 0
                })
                .ToListAsync();

            decimal monthOrderedQty = 0;
            var effectiveRows = new List<DashboardEffectiveRow>();

            foreach (var req in monthRequests)
            {
                var reqBaseRows = baseDetails
                    .Where(x => x.RequestHeaderId == req.Id)
                    .ToDictionary(
                        x => x.RequestDetailId,
                        x => new DashboardEffectiveRow
                        {
                            RequestDetailId = x.RequestDetailId,
                            SessionId = x.SessionId,
                            CuisineId = x.CuisineId,
                            LocationId = x.LocationId,
                            CompanyId = req.CompanyId,
                            Qty = x.Qty
                        });

                for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
                {
                    if (date < req.FromDate.Date || date > req.ToDate.Date)
                        continue;

                    var ov = monthOverrides
                        .Where(x => x.RequestHeaderId == req.Id &&
                                    x.FromDate.Date <= date &&
                                    x.ToDate.Date >= date)
                        .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();

                    var effectiveDetailRows = reqBaseRows.ToDictionary(
                        x => x.Key,
                        x => new DashboardEffectiveRow
                        {
                            RequestDetailId = x.Value.RequestDetailId,
                            SessionId = x.Value.SessionId,
                            CuisineId = x.Value.CuisineId,
                            LocationId = x.Value.LocationId,
                            CompanyId = x.Value.CompanyId,
                            Qty = x.Value.Qty
                        });

                    if (ov != null)
                    {
                        var ovRows = overrideDetails
                            .Where(x => x.RequestOverrideId == ov.Id)
                            .ToList();

                        foreach (var ovRow in ovRows)
                        {
                            if (effectiveDetailRows.ContainsKey(ovRow.RequestDetailId))
                            {
                                effectiveDetailRows[ovRow.RequestDetailId].Qty = ovRow.Qty;
                            }
                            else
                            {
                                effectiveDetailRows[ovRow.RequestDetailId] = new DashboardEffectiveRow
                                {
                                    RequestDetailId = ovRow.RequestDetailId,
                                    SessionId = ovRow.SessionId,
                                    CuisineId = ovRow.CuisineId,
                                    LocationId = ovRow.LocationId,
                                    CompanyId = req.CompanyId,
                                    Qty = ovRow.Qty
                                };
                            }
                        }
                    }

                    foreach (var row in effectiveDetailRows.Values)
                    {
                        monthOrderedQty += row.Qty;

                        effectiveRows.Add(new DashboardEffectiveRow
                        {
                            RequestDetailId = row.RequestDetailId,
                            SessionId = row.SessionId,
                            CuisineId = row.CuisineId,
                            LocationId = row.LocationId,
                            CompanyId = row.CompanyId,
                            Qty = row.Qty
                        });
                    }
                }
            }

            var sessionMaster = await _context.Session
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.SessionName
                })
                .ToListAsync();

            var companyMaster = await _context.CompanyMaster
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyName
                })
                .ToListAsync();

            var ordersBySession = effectiveRows
                .GroupBy(x => x.SessionId)
                .Select(g => new SessionOrderDTO
                {
                    SessionName = sessionMaster.FirstOrDefault(s => s.Id == g.Key)?.SessionName ?? "",
                    TotalQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.TotalQty)
                .ToList();

            var allCompanyWiseOrders = effectiveRows
                .GroupBy(x => x.CompanyId)
                .Select(g => new CompanyOrderDTO
                {
                    CompanyId = g.Key,
                    CompanyName = companyMaster.FirstOrDefault(c => c.Id == g.Key)?.CompanyName ?? "",
                    TotalQty = g.Sum(x => x.Qty),
                    RedeemQty = 0,
                    PendingQty = 0
                })
                .OrderByDescending(x => x.TotalQty)
                .ToList();

            var todayCount = await _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == today)
                .CountAsync();

            var yesterdayCount = await _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == yesterday)
                .CountAsync();

            var latestUsedQRs = await (
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                join c in _context.CompanyMaster on qr.CompanyId equals c.Id
                join s in _context.Session on log.SessionId equals s.Id
                where log.IsAllowed
                orderby log.ScanDateTime descending
                select new LatestQrDTO
                {
                    UniqueCode = log.UniqueCode ?? "",
                    CompanyName = c.CompanyName ?? "",
                    UsedDate = log.ScanDateTime,
                    SessionName = s.SessionName ?? ""
                }
            )
            .Take(4)
            .ToListAsync();

            var monthRedeemMap = await (
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                where log.IsAllowed &&
                      log.ScanDate >= monthStart &&
                      log.ScanDate <= monthEnd
                group log by qr.CompanyId into g
                select new
                {
                    CompanyId = g.Key,
                    RedeemQty = g.Count()
                }
            ).ToDictionaryAsync(x => x.CompanyId, x => x.RedeemQty);

            foreach (var item in allCompanyWiseOrders)
            {
                item.RedeemQty = monthRedeemMap.ContainsKey(item.CompanyId)
                    ? monthRedeemMap[item.CompanyId]
                    : 0;

                item.PendingQty = item.TotalQty - item.RedeemQty;

                if (item.PendingQty < 0)
                    item.PendingQty = 0;
            }

            var companyWiseOrders = allCompanyWiseOrders
                .OrderByDescending(x => x.TotalQty)
                .Take(8)
                .ToList();

            var monthRedeemedQty = allCompanyWiseOrders.Sum(x => x.RedeemQty);
            var monthPendingQty = allCompanyWiseOrders.Sum(x => x.PendingQty);

            decimal todayOrderedQty = 0;

            foreach (var req in monthRequests)
            {
                if (today < req.FromDate.Date || today > req.ToDate.Date)
                    continue;

                var reqBaseRows = baseDetails
                    .Where(x => x.RequestHeaderId == req.Id)
                    .ToDictionary(
                        x => x.RequestDetailId,
                        x => new DashboardEffectiveRow
                        {
                            RequestDetailId = x.RequestDetailId,
                            SessionId = x.SessionId,
                            CuisineId = x.CuisineId,
                            LocationId = x.LocationId,
                            CompanyId = req.CompanyId,
                            Qty = x.Qty
                        });

                var effectiveDetailRows = reqBaseRows.ToDictionary(
                    x => x.Key,
                    x => new DashboardEffectiveRow
                    {
                        RequestDetailId = x.Value.RequestDetailId,
                        SessionId = x.Value.SessionId,
                        CuisineId = x.Value.CuisineId,
                        LocationId = x.Value.LocationId,
                        CompanyId = x.Value.CompanyId,
                        Qty = x.Value.Qty
                    });

                var ov = monthOverrides
                    .Where(x => x.RequestHeaderId == req.Id &&
                                x.FromDate.Date <= today &&
                                x.ToDate.Date >= today)
                    .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault();

                if (ov != null)
                {
                    var ovRows = overrideDetails
                        .Where(x => x.RequestOverrideId == ov.Id)
                        .ToList();

                    foreach (var ovRow in ovRows)
                    {
                        if (effectiveDetailRows.ContainsKey(ovRow.RequestDetailId))
                        {
                            effectiveDetailRows[ovRow.RequestDetailId].Qty = ovRow.Qty;
                        }
                        else
                        {
                            effectiveDetailRows[ovRow.RequestDetailId] = new DashboardEffectiveRow
                            {
                                RequestDetailId = ovRow.RequestDetailId,
                                SessionId = ovRow.SessionId,
                                CuisineId = ovRow.CuisineId,
                                LocationId = ovRow.LocationId,
                                CompanyId = req.CompanyId,
                                Qty = ovRow.Qty
                            };
                        }
                    }
                }

                todayOrderedQty += effectiveDetailRows.Values.Sum(x => x.Qty);
            }

            decimal todayRedeemedQty = todayCount;
            decimal todayPendingQty = todayOrderedQty - todayRedeemedQty;

            if (todayPendingQty < 0)
                todayPendingQty = 0;

            return new DashboardDTO
            {
                TotalCompanies = totalCompanies,
                TotalOrders = totalOrders,
                TotalQRCodes = totalQrCodes,
                TodayScans = todayCount,
                YesterdayScans = yesterdayCount,
                TodayOrderedQty = todayOrderedQty,
                TodayRedeemedQty = todayRedeemedQty,
                TodayPendingQty = todayPendingQty,
                MonthOrderedQty = monthOrderedQty,
                MonthRedeemedQty = monthRedeemedQty,
                MonthPendingQty = monthPendingQty,
                TotalOrdersBySession = ordersBySession,
                TotalcompanyWiseOrders = companyWiseOrders,
                TotallatestUsedQRs = latestUsedQRs
            };
        }

        private class DashboardEffectiveRow
        {
            public int RequestDetailId { get; set; }
            public int SessionId { get; set; }
            public int CuisineId { get; set; }
            public int LocationId { get; set; }
            public int CompanyId { get; set; }
            public decimal Qty { get; set; }
        }
    }
}
