using CateringApi.Data;
using CateringApi.DTOModel;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Dashboard;
using CateringApi.DTOs.QR;
using CateringApi.DTOs.Session;
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

        public async Task<DashboardDTO> GetDashboardData(DashboardFilterDTO filter)
        {
            filter ??= new DashboardFilterDTO();

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var fromDate = filter.FromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
            var toDate = filter.ToDate?.Date ?? fromDate.AddMonths(1).AddDays(-1);

            var companyIds = filter.CompanyIds?.Distinct().ToList() ?? new List<int>();
            var sessionIds = filter.SessionIds?.Distinct().ToList() ?? new List<int>();
            var locationIds = filter.LocationIds?.Distinct().ToList() ?? new List<int>();

            var hasCompanyFilter = companyIds.Any();
            var hasSessionFilter = sessionIds.Any();
            var hasLocationFilter = locationIds.Any();

            var totalCompaniesQuery = _context.CompanyMaster.Where(x => x.IsActive);

            if (hasCompanyFilter)
                totalCompaniesQuery = totalCompaniesQuery.Where(x => companyIds.Contains(x.Id));

            var totalCompanies = await totalCompaniesQuery.CountAsync();

            var requestHeaderQuery = _context.RequestHeader
                .Where(r => r.IsActive &&
                            r.FromDate <= toDate &&
                            r.ToDate >= fromDate);

            if (hasCompanyFilter)
                requestHeaderQuery = requestHeaderQuery.Where(r => companyIds.Contains(r.CompanyId));

            var monthRequests = await requestHeaderQuery
                .Select(r => new
                {
                    r.Id,
                    r.CompanyId,
                    FromDate = r.FromDate.Date,
                    ToDate = r.ToDate.Date
                })
                .ToListAsync();

            var requestIds = monthRequests.Select(x => x.Id).ToList();
            var totalOrders = requestIds.Count;

            var qrRequestQuery = _context.QrCodeRequest
                .Where(x => x.IsActive &&
                            x.QRValidFrom <= toDate &&
                            x.QRValidTill >= fromDate);

            if (hasCompanyFilter)
                qrRequestQuery = qrRequestQuery.Where(x => companyIds.Contains(x.CompanyId));

            var totalQrCodes = await qrRequestQuery.SumAsync(x => (int?)x.NoofQR) ?? 0;

            var monthOverrides = await _context.RequestOverride
                .Where(o => o.IsActive &&
                            requestIds.Contains(o.RequestHeaderId) &&
                            o.FromDate <= toDate &&
                            o.ToDate >= fromDate)
                .Select(o => new
                {
                    o.Id,
                    o.RequestHeaderId,
                    FromDate = o.FromDate.Date,
                    ToDate = o.ToDate.Date,
                    o.CreatedDate,
                    o.UpdatedDate
                })
                .ToListAsync();

            var overrideIds = monthOverrides.Select(x => x.Id).ToList();

            var baseDetailsQuery = _context.RequestDetail
                .Where(x => x.IsActive && requestIds.Contains(x.RequestHeaderId));

            if (hasSessionFilter)
                baseDetailsQuery = baseDetailsQuery.Where(x =>
                    x.SessionId.HasValue && sessionIds.Contains(x.SessionId.Value));

            if (hasLocationFilter)
                baseDetailsQuery = baseDetailsQuery.Where(x =>
                    x.LocationId.HasValue && locationIds.Contains(x.LocationId.Value));

            var baseDetails = await baseDetailsQuery
                .Select(x => new
                {
                    RequestDetailId = x.Id,
                    x.RequestHeaderId,
                    SessionId = x.SessionId,
                    CuisineId = x.CuisineId,
                    LocationId = x.LocationId,
                    PlanType = x.PlanType ?? "",
                    Qty = (decimal?)x.Qty ?? 0
                })
                .ToListAsync();

            var overrideDetailsQuery =
                from rod in _context.RequestOverrideDetail
                join rd in _context.RequestDetail
                    on rod.RequestDetailId equals rd.Id into rdJoin
                from rd in rdJoin.DefaultIfEmpty()
                where rod.IsActive &&
                      overrideIds.Contains(rod.RequestOverrideId) &&
                      !rod.IsCancelled
                select new
                {
                    rod.RequestOverrideId,
                    RequestDetailId = rod.RequestDetailId,
                    SessionId = rd != null ? rd.SessionId : null,
                    CuisineId = rod.CuisineId,
                    LocationId = rd != null ? rd.LocationId : null,
                    PlanType = rod.PlanType ?? "",
                    Qty = (decimal?)rod.OverrideQty ?? 0
                };

            if (hasSessionFilter)
                overrideDetailsQuery = overrideDetailsQuery.Where(x =>
                    x.SessionId.HasValue && sessionIds.Contains(x.SessionId.Value));

            if (hasLocationFilter)
                overrideDetailsQuery = overrideDetailsQuery.Where(x =>
                    x.LocationId.HasValue && locationIds.Contains(x.LocationId.Value));

            var overrideDetails = await overrideDetailsQuery.ToListAsync();

            decimal orderedQty = 0;
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
                            PlanType = NormalizePlanType(x.PlanType),
                            Qty = x.Qty
                        });

                if (!reqBaseRows.Any())
                    continue;

                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    if (date < req.FromDate || date > req.ToDate)
                        continue;

                    var ov = monthOverrides
                        .Where(x => x.RequestHeaderId == req.Id &&
                                    x.FromDate <= date &&
                                    x.ToDate >= date)
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
                            PlanType = x.Value.PlanType,
                            Qty = x.Value.Qty
                        });

                    if (ov != null)
                    {
                        var ovRows = overrideDetails
                            .Where(x => x.RequestOverrideId == ov.Id)
                            .ToList();

                        foreach (var ovRow in ovRows)
                        {
                            var key = ovRow.RequestDetailId > 0
                                ? ovRow.RequestDetailId
                                : -1 * ovRow.CuisineId;

                            if (effectiveDetailRows.ContainsKey(key))
                            {
                                effectiveDetailRows[key].Qty = ovRow.Qty;
                                effectiveDetailRows[key].PlanType = NormalizePlanType(ovRow.PlanType);
                            }
                            else
                            {
                                effectiveDetailRows[key] = new DashboardEffectiveRow
                                {
                                    RequestDetailId = ovRow.RequestDetailId,
                                    SessionId = ovRow.SessionId,
                                    CuisineId = ovRow.CuisineId,
                                    LocationId = ovRow.LocationId,
                                    CompanyId = req.CompanyId,
                                    PlanType = NormalizePlanType(ovRow.PlanType),
                                    Qty = ovRow.Qty
                                };
                            }
                        }
                    }

                    foreach (var row in effectiveDetailRows.Values)
                    {
                        orderedQty += row.Qty;

                        effectiveRows.Add(new DashboardEffectiveRow
                        {
                            RequestDetailId = row.RequestDetailId,
                            SessionId = row.SessionId,
                            CuisineId = row.CuisineId,
                            LocationId = row.LocationId,
                            CompanyId = row.CompanyId,
                            PlanType = NormalizePlanType(row.PlanType),
                            Qty = row.Qty,
                            OrderDate = date
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
                .Where(x => x.SessionId.HasValue)
                .GroupBy(x => x.SessionId!.Value)
                .Select(g => new SessionOrderDTO
                {
                    SessionName = sessionMaster.FirstOrDefault(s => s.Id == g.Key)?.SessionName ?? "",
                    TotalQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.TotalQty)
                .ToList();

            var ordersByPlanType = effectiveRows
                .GroupBy(x => NormalizePlanType(x.PlanType))
                .Select(g => new PlanTypeOrderDTO
                {
                    PlanType = g.Key,
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

            var todayScanQuery = _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == today);

            var yesterdayScanQuery = _context.QrScanLog
                .Where(x => x.IsAllowed && x.ScanDate == yesterday);

            var qrScanQuery = _context.QrScanLog
                .Where(x => x.IsAllowed &&
                            x.ScanDate >= fromDate &&
                            x.ScanDate <= toDate);

            if (hasCompanyFilter)
            {
                var companyQrIds = await _context.QrCodeRequest
                    .Where(x => companyIds.Contains(x.CompanyId))
                    .Select(x => x.Id)
                    .ToListAsync();

                todayScanQuery = todayScanQuery.Where(x => companyQrIds.Contains(x.QrCodeRequestId));
                yesterdayScanQuery = yesterdayScanQuery.Where(x => companyQrIds.Contains(x.QrCodeRequestId));
                qrScanQuery = qrScanQuery.Where(x => companyQrIds.Contains(x.QrCodeRequestId));
            }

            if (hasSessionFilter)
            {
                todayScanQuery = todayScanQuery.Where(x => sessionIds.Contains(x.SessionId));
                yesterdayScanQuery = yesterdayScanQuery.Where(x => sessionIds.Contains(x.SessionId));
                qrScanQuery = qrScanQuery.Where(x => sessionIds.Contains(x.SessionId));
            }

            var todayCount = await todayScanQuery.CountAsync();
            var yesterdayCount = await yesterdayScanQuery.CountAsync();

            var latestUsedQRsQuery =
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                join c in _context.CompanyMaster on qr.CompanyId equals c.Id
                join s in _context.Session on log.SessionId equals s.Id
                where log.IsAllowed &&
                      log.ScanDate >= fromDate &&
                      log.ScanDate <= toDate
                select new { log, qr, c, s };

            if (hasCompanyFilter)
                latestUsedQRsQuery = latestUsedQRsQuery.Where(x => companyIds.Contains(x.qr.CompanyId));

            if (hasSessionFilter)
                latestUsedQRsQuery = latestUsedQRsQuery.Where(x => sessionIds.Contains(x.log.SessionId));

            var latestUsedQRs = await latestUsedQRsQuery
                .OrderByDescending(x => x.log.ScanDateTime)
                .Take(4)
                .Select(x => new LatestQrDTO
                {
                    UniqueCode = x.log.UniqueCode ?? "",
                    CompanyName = x.c.CompanyName ?? "",
                    UsedDate = x.log.ScanDateTime,
                    SessionName = x.s.SessionName ?? ""
                })
                .ToListAsync();

            var redeemBaseQuery =
                from log in _context.QrScanLog
                join qr in _context.QrCodeRequest on log.QrCodeRequestId equals qr.Id
                where log.IsAllowed &&
                      log.ScanDate >= fromDate &&
                      log.ScanDate <= toDate
                select new { log, qr };

            if (hasCompanyFilter)
                redeemBaseQuery = redeemBaseQuery.Where(x => companyIds.Contains(x.qr.CompanyId));

            if (hasSessionFilter)
                redeemBaseQuery = redeemBaseQuery.Where(x => sessionIds.Contains(x.log.SessionId));

            var monthRedeemMap = await redeemBaseQuery
                .GroupBy(x => x.qr.CompanyId)
                .Select(g => new
                {
                    CompanyId = g.Key,
                    RedeemQty = g.Count()
                })
                .ToDictionaryAsync(x => x.CompanyId, x => x.RedeemQty);

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

            decimal todayOrderedQty = effectiveRows
                .Where(x => x.OrderDate.Date == today)
                .Sum(x => x.Qty);

            decimal todayRedeemedQty = todayCount;
            decimal todayPendingQty = todayOrderedQty - todayRedeemedQty;

            if (todayPendingQty < 0)
                todayPendingQty = 0;

            var sessionPriceHistoryQuery =
                from h in _context.SessionPriceHistory
                where h.EffectiveFrom <= toDate
                select new
                {
                    h.CompanyId,
                    h.SessionId,
                    h.Rate,
                    EffectiveFrom = h.EffectiveFrom.Date,
                    EffectiveTo = h.EffectiveTo.HasValue ? h.EffectiveTo.Value.Date : (DateTime?)null
                };

            if (hasCompanyFilter)
                sessionPriceHistoryQuery = sessionPriceHistoryQuery
                    .Where(x => companyIds.Contains(x.CompanyId));

            if (hasSessionFilter)
                sessionPriceHistoryQuery = sessionPriceHistoryQuery
                    .Where(x => sessionIds.Contains(x.SessionId));

            var sessionPriceHistory = await sessionPriceHistoryQuery.ToListAsync();

            var currentSessionPricesQuery =
                from p in _context.SessionPrice
                join c in _context.CompanyMaster on p.CompanyId equals c.Id
                join s in _context.Session on p.SessionId equals s.Id
                where p.IsActive
                select new DashboardPriceDto
                {
                    PriceId = p.Id,
                    CompanyId = p.CompanyId,
                    CompanyName = c.CompanyName ?? "",
                    SessionId = p.SessionId,
                    SessionName = s.SessionName ?? "",
                    Rate = p.Rate,
                    EffectiveFrom = p.EffectiveFrom
                };

            if (hasCompanyFilter)
                currentSessionPricesQuery = currentSessionPricesQuery
                    .Where(x => companyIds.Contains(x.CompanyId));

            if (hasSessionFilter)
                currentSessionPricesQuery = currentSessionPricesQuery
                    .Where(x => sessionIds.Contains(x.SessionId));

            var currentSessionPrices = await currentSessionPricesQuery
                .OrderBy(x => x.CompanyName)
                .ThenBy(x => x.SessionName)
                .ToListAsync();

            decimal totalPrice = 0;
            var sessionTotalMap = new Dictionary<int, decimal>();

            foreach (var row in effectiveRows)
            {
                if (!row.SessionId.HasValue)
                    continue;

                var matchedRate = sessionPriceHistory
                    .Where(x => x.CompanyId == row.CompanyId
                             && x.SessionId == row.SessionId.Value
                             && x.EffectiveFrom <= row.OrderDate
                             && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= row.OrderDate))
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefault();

                if (matchedRate != null)
                {
                    var rowTotal = row.Qty * matchedRate.Rate;

                    totalPrice += rowTotal;

                    if (!sessionTotalMap.ContainsKey(row.SessionId.Value))
                        sessionTotalMap[row.SessionId.Value] = 0;

                    sessionTotalMap[row.SessionId.Value] += rowTotal;
                }
            }

            var sessionPriceBreakdown = sessionTotalMap
                .Select(x => new SessionPriceBreakdownDTO
                {
                    SessionId = x.Key,
                    SessionName = sessionMaster.FirstOrDefault(s => s.Id == x.Key)?.SessionName ?? "",
                    TotalPrice = x.Value
                })
                .OrderBy(x => x.SessionName)
                .ToList();

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
                MonthOrderedQty = orderedQty,
                MonthRedeemedQty = monthRedeemedQty,
                MonthPendingQty = monthPendingQty,
                TotalOrdersBySession = ordersBySession,
                TotalOrdersByPlanType = ordersByPlanType,
                TotalcompanyWiseOrders = companyWiseOrders,
                TotallatestUsedQRs = latestUsedQRs,
                CurrentSessionPrices = currentSessionPrices,
                TotalPrice = totalPrice,
                SessionPriceBreakdown = sessionPriceBreakdown
            };
        }

        private class DashboardEffectiveRow
        {
            public int RequestDetailId { get; set; }
            public int? SessionId { get; set; }
            public int CuisineId { get; set; }
            public int? LocationId { get; set; }
            public int CompanyId { get; set; }
            public string PlanType { get; set; } = "";
            public decimal Qty { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class SessionPriceBreakdownDTO
        {
            public int SessionId { get; set; }
            public string SessionName { get; set; }
            public decimal TotalPrice { get; set; }
        }
        public class PlanTypeOrderDTO
        {
            public string PlanType { get; set; } = "";
            public decimal TotalQty { get; set; }
        }
        private string NormalizePlanType(string? value)
        {
            var text = (value ?? "").Trim().ToLower();

            if (text == "basic") return "Basic";
            if (text == "standard") return "Standard";
            if (text == "premium") return "Premium";

            return "Basic";
        }
    }
}