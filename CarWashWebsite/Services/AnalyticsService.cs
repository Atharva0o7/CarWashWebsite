using CarWashWebsite.Data;
using CarWashWebsite.Models;
using CarWashWebsite.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Services;

/// <summary>
/// Read-only aggregates for the admin dashboard. Every figure is derived from the
/// bookings table so the numbers can always be reconciled against the raw rows.
/// </summary>
public class AnalyticsService(AppDbContext db)
{
    /// <summary>Statuses that represent money we expect to actually collect.</summary>
    private static readonly BookingStatus[] LiveStatuses =
        [BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed];

    public async Task<DashboardViewModel> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekEnd = today.AddDays(7);

        // One filtered set drives every aggregate below.
        var window = db.Bookings.AsNoTracking()
            .Where(b => b.PreferredDate >= from && b.PreferredDate <= to);

        // EF cannot translate a GroupBy projected straight into a positional record
        // constructor, so every aggregate lands in an anonymous type first and is
        // mapped to its record after materialising.
        var byStatus = (await window
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(x => (long)x.QuotedPrice) })
            .ToListAsync(ct))
            .Select(r => new StatusSlice(r.Status, r.Count, r.Revenue))
            .ToList();

        var live = byStatus.Where(s => LiveStatuses.Contains(s.Status)).ToList();
        var liveCount = live.Sum(s => s.Count);
        var revenueBooked = live.Sum(s => s.Revenue);
        var cancelled = byStatus.FirstOrDefault(s => s.Status == BookingStatus.Cancelled)?.Count ?? 0;
        var allInWindow = byStatus.Sum(s => s.Count);

        var trend = (await window
            .GroupBy(b => b.PreferredDate)
            .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(x => (long)x.QuotedPrice) })
            .OrderBy(r => r.Date)
            .ToListAsync(ct))
            .Select(r => new TrendPoint(r.Date, r.Count, r.Revenue))
            .ToList();

        var topServices = await NamedAsync(window.GroupBy(b => b.Service!.Name), take: 8, ct);
        var topLocalities = await NamedAsync(window.GroupBy(b => b.Locality), take: 8, ct);
        var byBodyType = await NamedAsync(window.GroupBy(b => b.BodyType), take: null, ct);
        var bySlot = await NamedAsync(window.GroupBy(b => b.PreferredSlot), take: null, ct);

        var upcoming = await db.Bookings.AsNoTracking()
            .Include(b => b.Service)
            .Where(b => b.PreferredDate >= today
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Completed)
            .OrderBy(b => b.PreferredDate).ThenBy(b => b.PreferredSlot)
            .Take(12)
            .ToListAsync(ct);

        return new DashboardViewModel
        {
            From = from,
            To = to,
            JobsToday = await db.Bookings.CountAsync(
                b => b.PreferredDate == today && b.Status != BookingStatus.Cancelled, ct),
            JobsThisWeek = await db.Bookings.CountAsync(
                b => b.PreferredDate >= today && b.PreferredDate < weekEnd
                  && b.Status != BookingStatus.Cancelled, ct),
            PendingCount = await db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending, ct),
            TotalBookings = await db.Bookings.CountAsync(ct),

            RevenueBooked = revenueBooked,
            RevenueCompleted = byStatus
                .FirstOrDefault(s => s.Status == BookingStatus.Completed)?.Revenue ?? 0,
            AverageTicket = liveCount == 0 ? 0 : (int)(revenueBooked / liveCount),
            CancellationRate = allInWindow == 0 ? 0 : Math.Round(cancelled * 100d / allInWindow, 1),

            ByStatus = [.. byStatus.OrderBy(s => s.Status)],
            Trend = trend,
            TopServices = topServices,
            TopLocalities = topLocalities,
            ByBodyType = byBodyType,
            // Chronological, not by volume — slot load reads better in time order.
            BySlot = [.. bySlot.OrderBy(r => r.Name)],
            UpcomingJobs = upcoming,
        };
    }

    /// <summary>
    /// Counts and sums a grouping into <see cref="NamedCount"/>, ordered by volume.
    /// Shared by the service / locality / body-type / slot breakdowns.
    /// </summary>
    private static async Task<List<NamedCount>> NamedAsync(
        IQueryable<IGrouping<string, Booking>> grouped, int? take, CancellationToken ct)
    {
        var query = grouped
            .Select(g => new { Name = g.Key, Count = g.Count(), Revenue = g.Sum(x => (long)x.QuotedPrice) })
            .OrderByDescending(r => r.Count);

        var rows = take is { } n
            ? await query.Take(n).ToListAsync(ct)
            : await query.ToListAsync(ct);

        return [.. rows.Select(r => new NamedCount(r.Name, r.Count, r.Revenue))];
    }
}
