using CarWashWebsite.Models;
using CarWashWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWashWebsite.Controllers;

[ApiController]
[Route("api")]
public class BookingsController(
    CatalogService catalog,
    BookingService bookings) : ControllerBase
{
    /// <summary>Creates a booking. Called by the /book page via fetch, and available as a JSON API.</summary>
    [HttpPost("bookings")]
    [ProducesResponseType<BookingResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingResult>> Create(BookingRequest request, CancellationToken ct)
    {
        if (await catalog.FindServiceAsync(request.ServiceSlug, ct) is null)
        {
            ModelState.AddModelError(nameof(request.ServiceSlug), "Unknown service.");
            return ValidationProblem(ModelState);
        }

        if (request.PreferredDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.PreferredDate), "Pick today or a later date.");
            return ValidationProblem(ModelState);
        }

        var result = await bookings.CreateAsync(request, ct);
        return Created($"/api/bookings/{result.Reference}", result);
    }

    /// <summary>
    /// Staff-only. Booking references are short enough to enumerate, so this must not be
    /// public — it would leak customer names, numbers and addresses.
    /// </summary>
    [HttpGet("bookings/{reference}")]
    [Authorize(Policy = Policies.StaffOrAdmin)]
    [ProducesResponseType<Booking>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> Get(string reference, CancellationToken ct)
    {
        var booking = await bookings.FindAsync(reference, ct);
        if (booking is null)
        {
            return NotFound(new { message = $"No booking with reference '{reference}'." });
        }

        return Ok(new
        {
            booking.Reference,
            booking.Status,
            service = booking.Service?.Name,
            car = $"{booking.CarBrand} {booking.CarModel}",
            booking.BodyType,
            booking.Locality,
            booking.PreferredDate,
            booking.PreferredSlot,
            booking.QuotedPrice,
            booking.CreatedAt,
        });
    }

    /// <summary>Live price for a service against a body type — powers the hero price picker.</summary>
    [HttpGet("quote")]
    public async Task<ActionResult<object>> Quote(string serviceSlug, string bodyType, CancellationToken ct)
    {
        var service = await catalog.FindServiceAsync(serviceSlug, ct);
        if (service is null)
        {
            return NotFound(new { message = "Unknown service." });
        }

        if (!CatalogService.BodyTypeMultipliers.ContainsKey(bodyType))
        {
            return BadRequest(new { message = "Unknown body type." });
        }

        return Ok(new
        {
            service = service.Name,
            slug = service.Slug,
            bodyType,
            price = CatalogService.PriceFor(service.StartingPrice, bodyType),
            listPrice = CatalogService.PriceFor(service.ListPrice, bodyType),
            savingsPercent = service.SavingsPercent,
            duration = service.Duration,
            warranty = service.Warranty,
            includes = service.Includes.Select(i => i.Text),
        });
    }
}
