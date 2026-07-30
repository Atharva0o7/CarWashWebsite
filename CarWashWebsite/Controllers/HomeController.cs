using CarWashWebsite.Models;
using CarWashWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarWashWebsite.Controllers;

public class HomeController(CatalogService catalog, BookingService bookings) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var services = await catalog.GetServicesAsync(ct);

        ViewData["Title"] = $"Car Wash & Detailing in {CatalogService.DefaultCity} — PitStop Autocare";
        ViewData["ActiveNav"] = "home";

        return View(new HomeViewModel
        {
            City = CatalogService.DefaultCity,
            Services = services,
            // Biggest discounts drive the "monsoon ready" carousel.
            FeaturedOffers = [.. services.Where(s => s.SavingsPercent >= 35).Take(6)],
            Plans = await catalog.GetPlansAsync(ct),
            Testimonials = await catalog.GetTestimonialsAsync(ct),
            Faqs = await catalog.GetFaqsAsync(ct),
            Brands = await catalog.GetBrandsAsync(ct),
            Localities = await catalog.GetLocalityNamesAsync(ct: ct),
        });
    }

    [HttpGet("/services")]
    public async Task<IActionResult> Services(CancellationToken ct)
    {
        ViewData["Title"] = "All Car Wash & Detailing Services — PitStop Autocare";
        ViewData["ActiveNav"] = "services";
        return View(await catalog.GetServicesAsync(ct));
    }

    [HttpGet("/services/{slug}")]
    public async Task<IActionResult> Service(string slug, CancellationToken ct)
    {
        var service = await catalog.FindServiceAsync(slug, ct);
        if (service is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"{service.Name} in {CatalogService.DefaultCity} — PitStop Autocare";
        ViewData["ActiveNav"] = "services";
        return View(service);
    }

    [HttpGet("/plans")]
    public async Task<IActionResult> Plans(CancellationToken ct)
    {
        ViewData["Title"] = "Monthly Car Wash Plans in Pune — PitStop Autocare";
        ViewData["ActiveNav"] = "plans";
        return View(await catalog.GetPlansAsync(ct));
    }

    [HttpGet("/book")]
    public async Task<IActionResult> Book(string? service, string? locality, string? plan, CancellationToken ct)
    {
        ViewData["Title"] = "Book a Car Wash — PitStop Autocare";
        ViewData["ActiveNav"] = "book";

        var services = await catalog.GetServicesAsync(ct);
        var localities = await catalog.GetLocalityNamesAsync(ct: ct);

        // Plans are sold on the confirmation call, so a plan link books a single wash
        // and carries the interest through in the notes rather than dropping it.
        string? planInterest = null;
        if (!string.IsNullOrWhiteSpace(plan))
        {
            var plans = await catalog.GetPlansAsync(ct);
            planInterest = plans.FirstOrDefault(p => p.Name == plan)?.Name;
            ViewData["PlanInterest"] = planInterest;
        }

        return View(new BookingPageViewModel
        {
            Brands = await catalog.GetBrandsAsync(ct),
            Services = services,
            Localities = localities,
            TimeSlots = [.. CatalogService.TimeSlots],
            Form = new BookingRequest
            {
                ServiceSlug = services.FirstOrDefault(s => s.Slug == service)?.Slug
                              ?? services.FirstOrDefault()?.Slug
                              ?? "",
                Locality = localities.FirstOrDefault(l => l == locality) ?? "",
                PreferredDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Notes = planInterest is null ? null : $"Interested in the {planInterest} plan.",
            },
        });
    }

    /// <summary>Non-JS fallback for the booking form — the page posts via fetch when JS is on.</summary>
    [HttpPost("/book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingRequest form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Book a Car Wash — PitStop Autocare";
            ViewData["ActiveNav"] = "book";

            return View(new BookingPageViewModel
            {
                Form = form,
                Brands = await catalog.GetBrandsAsync(ct),
                Services = await catalog.GetServicesAsync(ct),
                Localities = await catalog.GetLocalityNamesAsync(ct: ct),
                TimeSlots = [.. CatalogService.TimeSlots],
            });
        }

        var result = await bookings.CreateAsync(form, ct);
        return RedirectToAction(nameof(Confirmed), new { reference = result.Reference });
    }

    [HttpGet("/book/confirmed/{reference}")]
    public async Task<IActionResult> Confirmed(string reference, CancellationToken ct)
    {
        var booking = await bookings.FindAsync(reference, ct);
        if (booking is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Booking {booking.Reference} confirmed — PitStop Autocare";
        return View(booking);
    }

    [HttpGet("/error")]
    [HttpGet("/error/{code:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error(int? code)
    {
        ViewData["Title"] = code == 404 ? "Page not found" : "Something went wrong";
        Response.StatusCode = code is >= 400 and < 600 ? code.Value : 500;
        return View(code ?? 500);
    }
}
