using CarWashWebsite.Data;
using CarWashWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Services;

/// <summary>Read-side access to the catalogue tables, plus the body-type pricing rules.</summary>
public class CatalogService(AppDbContext db)
{
    public const string DefaultCity = "Pune";

    /// <summary>Price multipliers applied on top of the hatchback base price.</summary>
    public static readonly IReadOnlyDictionary<string, double> BodyTypeMultipliers =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hatchback"] = 1.0,
            ["Sedan"] = 1.15,
            ["SUV"] = 1.35,
            ["Luxury"] = 1.75,
        };

    public static readonly string[] TimeSlots =
    [
        "08:00 – 10:00 AM", "10:00 – 12:00 PM", "12:00 – 02:00 PM",
        "02:00 – 04:00 PM", "04:00 – 06:00 PM", "06:00 – 08:00 PM",
    ];

    /// <summary>Rounds to the nearest ₹10 so quotes read like real price-list numbers.</summary>
    public static int PriceFor(int basePrice, string bodyType)
    {
        var multiplier = BodyTypeMultipliers.TryGetValue(bodyType, out var m) ? m : 1.0;
        return (int)(Math.Round(basePrice * multiplier / 10d) * 10);
    }

    public Task<List<Service>> GetServicesAsync(CancellationToken ct = default) =>
        db.Services
          .AsNoTracking()
          .Include(s => s.Includes.OrderBy(i => i.SortOrder))
          .OrderBy(s => s.SortOrder)
          .ToListAsync(ct);

    public Task<Service?> FindServiceAsync(string slug, CancellationToken ct = default) =>
        db.Services
          .AsNoTracking()
          .Include(s => s.Includes.OrderBy(i => i.SortOrder))
          .FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public Task<List<WashPlan>> GetPlansAsync(CancellationToken ct = default) =>
        db.WashPlans
          .AsNoTracking()
          .Include(p => p.Features.OrderBy(f => f.SortOrder))
          .OrderBy(p => p.SortOrder)
          .ToListAsync(ct);

    public Task<List<Testimonial>> GetTestimonialsAsync(CancellationToken ct = default) =>
        db.Testimonials.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(ct);

    public Task<List<FaqItem>> GetFaqsAsync(CancellationToken ct = default) =>
        db.Faqs.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct);

    public Task<List<CarBrand>> GetBrandsAsync(CancellationToken ct = default) =>
        db.CarBrands
          .AsNoTracking()
          .Include(b => b.Models.OrderBy(m => m.Name))
          .OrderBy(b => b.SortOrder)
          .ToListAsync(ct);

    public Task<List<string>> GetLocalityNamesAsync(string city = DefaultCity, CancellationToken ct = default) =>
        db.Localities
          .AsNoTracking()
          .Where(l => l.City == city && l.IsActive)
          .OrderBy(l => l.SortOrder)
          .Select(l => l.Name)
          .ToListAsync(ct);

    /// <summary>Body type for a specific model, used to price a booking server-side.</summary>
    public async Task<string> ResolveBodyTypeAsync(string brand, string model, CancellationToken ct = default)
    {
        var bodyType = await db.CarModels
            .AsNoTracking()
            .Where(m => m.Brand!.Name == brand && m.Name == model)
            .Select(m => m.BodyType)
            .FirstOrDefaultAsync(ct);

        return bodyType ?? "Hatchback";
    }
}
