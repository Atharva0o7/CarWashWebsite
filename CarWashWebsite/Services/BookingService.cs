using CarWashWebsite.Data;
using CarWashWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Services;

public class BookingService(AppDbContext db, CatalogService catalog, ILogger<BookingService> logger)
{
    /// <summary>
    /// Persists a booking and returns its reference. Price is recalculated server-side from the
    /// service row and the car's body type — never trusted from the form.
    /// </summary>
    public async Task<BookingResult> CreateAsync(BookingRequest request, CancellationToken ct = default)
    {
        var service = await db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == request.ServiceSlug, ct)
            ?? throw new ArgumentException($"Unknown service '{request.ServiceSlug}'.", nameof(request));

        var bodyType = await catalog.ResolveBodyTypeAsync(request.CarBrand, request.CarModel, ct);

        var booking = new Booking
        {
            Reference = await GenerateReferenceAsync(ct),
            CustomerName = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            CarBrand = request.CarBrand,
            CarModel = request.CarModel,
            BodyType = bodyType,
            ServiceId = service.Id,
            Locality = request.Locality,
            PreferredDate = request.PreferredDate,
            PreferredSlot = request.PreferredSlot,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            QuotedPrice = CatalogService.PriceFor(service.StartingPrice, bodyType),
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Booking {Reference}: {Service} for {Brand} {Model} ({BodyType}) at {Locality} on {Date} {Slot} — ₹{Price}",
            booking.Reference, service.Slug, booking.CarBrand, booking.CarModel, bodyType,
            booking.Locality, booking.PreferredDate, booking.PreferredSlot, booking.QuotedPrice);

        return new BookingResult
        {
            Reference = booking.Reference,
            QuotedPrice = booking.QuotedPrice,
            Message = $"{service.Name} booked for your {booking.CarBrand} {booking.CarModel} at ₹{booking.QuotedPrice:N0}. "
                    + $"We will call {booking.Phone} within 15 minutes to confirm your {booking.PreferredSlot} slot.",
        };
    }

    public Task<Booking?> FindAsync(string reference, CancellationToken ct = default) =>
        db.Bookings
          .AsNoTracking()
          .Include(b => b.Service)
          .FirstOrDefaultAsync(b => b.Reference == reference, ct);

    /// <summary>
    /// Builds a PIT-yyMMdd-#### reference, retrying on the (very unlikely) collision.
    /// The unique index on <c>bookings.reference</c> is the real guard.
    /// </summary>
    private async Task<string> GenerateReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"PIT-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 10000)}";
            if (!await db.Bookings.AnyAsync(b => b.Reference == candidate, ct))
            {
                return candidate;
            }
        }

        return $"PIT-{DateTime.Now:yyMMddHHmmss}";
    }
}
