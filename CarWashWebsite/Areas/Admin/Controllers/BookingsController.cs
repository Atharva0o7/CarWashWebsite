using CarWashWebsite.Models;
using CarWashWebsite.Models.Admin;
using CarWashWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/bookings")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class BookingsController(
    BookingAdminService bookings,
    CatalogService catalog,
    ExportService exports) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] BookingFilter filter, CancellationToken ct)
    {
        // Keep the page size sane regardless of what arrives in the query string.
        filter.PageSize = Math.Clamp(filter.PageSize, 10, 100);

        ViewData["Title"] = "Bookings — PitStop Admin";
        ViewData["ActiveNav"] = "bookings";
        return View(await bookings.ListAsync(filter, catalog, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var booking = await bookings.FindAsync(id, ct);
        if (booking is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"{booking.Reference} — PitStop Admin";
        ViewData["ActiveNav"] = "bookings";
        return View(booking);
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(
        int id, BookingStatus status, string? returnUrl, CancellationToken ct)
    {
        var actor = User.Identity?.Name ?? "unknown";
        var ok = await bookings.SetStatusAsync(id, status, actor, ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Flash"] = $"Booking moved to {status}.";

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Export([FromQuery] BookingFilter filter, CancellationToken ct)
    {
        var (content, fileName) = await exports.BookingsWorkbookAsync(filter, ct);

        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
