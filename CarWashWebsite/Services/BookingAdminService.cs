using CarWashWebsite.Data;
using CarWashWebsite.Models;
using CarWashWebsite.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Services;

/// <summary>Query and mutate bookings from the admin queue.</summary>
public class BookingAdminService(AppDbContext db, ILogger<BookingAdminService> logger)
{
    /// <summary>Applies the filter without paging — shared by the list view and the export.</summary>
    public IQueryable<Booking> Query(BookingFilter filter)
    {
        var q = db.Bookings.AsNoTracking().Include(b => b.Service).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var term = filter.Q.Trim();
            q = q.Where(b =>
                EF.Functions.ILike(b.Reference, $"%{term}%") ||
                EF.Functions.ILike(b.CustomerName, $"%{term}%") ||
                EF.Functions.ILike(b.Phone, $"%{term}%") ||
                EF.Functions.ILike(b.CarModel, $"%{term}%") ||
                EF.Functions.ILike(b.CarBrand, $"%{term}%"));
        }

        if (filter.Status is { } status) q = q.Where(b => b.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Locality)) q = q.Where(b => b.Locality == filter.Locality);
        if (!string.IsNullOrWhiteSpace(filter.ServiceSlug)) q = q.Where(b => b.Service!.Slug == filter.ServiceSlug);
        if (filter.From is { } from) q = q.Where(b => b.PreferredDate >= from);
        if (filter.To is { } to) q = q.Where(b => b.PreferredDate <= to);

        return q.OrderByDescending(b => b.PreferredDate).ThenByDescending(b => b.Id);
    }

    public async Task<BookingListViewModel> ListAsync(
        BookingFilter filter, CatalogService catalog, CancellationToken ct = default)
    {
        var q = Query(filter);

        var total = await q.CountAsync(ct);
        var value = total == 0 ? 0 : await q.SumAsync(b => (long)b.QuotedPrice, ct);

        var page = Math.Max(1, filter.Page);
        var rows = await q
            .Skip((page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new BookingListViewModel
        {
            Filter = filter,
            Bookings = rows,
            TotalCount = total,
            TotalValue = value,
            Localities = await catalog.GetLocalityNamesAsync(ct: ct),
            Services = await catalog.GetServicesAsync(ct),
        };
    }

    public Task<Booking?> FindAsync(int id, CancellationToken ct = default) =>
        db.Bookings.Include(b => b.Service).FirstOrDefaultAsync(b => b.Id == id, ct);

    /// <summary>Moves a booking to a new status. Returns false if the id is unknown.</summary>
    public async Task<bool> SetStatusAsync(int id, BookingStatus status, string actor, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        var previous = booking.Status;
        booking.Status = status;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Booking {Reference} moved {From} → {To} by {Actor}",
            booking.Reference, previous, status, actor);

        return true;
    }
}
