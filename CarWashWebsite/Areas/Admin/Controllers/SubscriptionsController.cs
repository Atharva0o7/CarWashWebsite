using System.ComponentModel.DataAnnotations;
using CarWashWebsite.Models;
using CarWashWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/subscriptions")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class SubscriptionsController(
    SubscriptionService subscriptions,
    CatalogService catalog,
    Data.AppDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(SubscriptionStatus? status, CancellationToken ct)
    {
        ViewData["Title"] = "Subscriptions — PitStop Admin";
        ViewData["ActiveNav"] = "subscriptions";

        return View(new SubscriptionPageViewModel
        {
            Status = status,
            Subscriptions = await subscriptions.ListAsync(status, ct),
            Plans = await db.WashPlans.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync(ct),
            Localities = await catalog.GetLocalityNamesAsync(ct: ct),
            Form = new SubscriptionInput { StartDate = DateOnly.FromDateTime(DateTime.Today) },
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionInput form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Subscriptions — PitStop Admin";
            ViewData["ActiveNav"] = "subscriptions";

            return View(nameof(Index), new SubscriptionPageViewModel
            {
                Status = null,
                Subscriptions = await subscriptions.ListAsync(null, ct),
                Plans = await db.WashPlans.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync(ct),
                Localities = await catalog.GetLocalityNamesAsync(ct: ct),
                Form = form,
            });
        }

        var bodyType = await catalog.ResolveBodyTypeAsync(form.CarBrand, form.CarModel, ct);

        var created = await subscriptions.CreateAsync(new Subscription
        {
            CustomerName = form.CustomerName.Trim(),
            Phone = form.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim(),
            CarBrand = form.CarBrand.Trim(),
            CarModel = form.CarModel.Trim(),
            BodyType = bodyType,
            RegistrationNumber = string.IsNullOrWhiteSpace(form.RegistrationNumber)
                ? null : form.RegistrationNumber.Trim().ToUpperInvariant(),
            WashPlanId = form.WashPlanId,
            Locality = form.Locality,
            StartDate = form.StartDate,
            WhatsAppOptIn = form.WhatsAppOptIn,
        }, ct);

        TempData["Flash"] = $"{created.Reference} created for {created.CustomerName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, SubscriptionStatus status, CancellationToken ct)
    {
        if (!await subscriptions.SetStatusAsync(id, status, ct))
        {
            return NotFound();
        }

        TempData["Flash"] = $"Subscription moved to {status}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/optin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOptIn(int id, bool optIn, CancellationToken ct)
    {
        if (!await subscriptions.SetOptInAsync(id, optIn, ct))
        {
            return NotFound();
        }

        TempData["Flash"] = optIn
            ? "WhatsApp reminders switched on for this customer."
            : "WhatsApp reminders switched off — they will get nothing from now on.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/wash")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordWash(int id, CancellationToken ct)
    {
        if (!await subscriptions.RecordWashAsync(id, DateOnly.FromDateTime(DateTime.Today), ct))
        {
            return NotFound();
        }

        TempData["Flash"] = "Wash recorded against the plan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("roll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RollPeriods(CancellationToken ct)
    {
        var count = await subscriptions.RollLapsedPeriodsAsync(ct);
        TempData["Flash"] = count == 0
            ? "No periods needed rolling."
            : $"Rolled {count} subscription period(s) into a new month.";
        return RedirectToAction(nameof(Index));
    }
}

public class SubscriptionPageViewModel
{
    public required SubscriptionStatus? Status { get; init; }
    public required List<Subscription> Subscriptions { get; init; }
    public required List<WashPlan> Plans { get; init; }
    public required List<string> Localities { get; init; }
    public required SubscriptionInput Form { get; init; }
}

public class SubscriptionInput
{
    [Required(ErrorMessage = "Enter the customer's name.")]
    [StringLength(80, MinimumLength = 2)]
    [Display(Name = "Customer name")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "A mobile number is required for reminders.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = "";

    [EmailAddress]
    [StringLength(160)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Pick a plan.")]
    public int WashPlanId { get; set; }

    [Required(ErrorMessage = "Enter the car brand.")]
    [StringLength(80)]
    public string CarBrand { get; set; } = "";

    [Required(ErrorMessage = "Enter the car model.")]
    [StringLength(80)]
    public string CarModel { get; set; } = "";

    [StringLength(20)]
    [Display(Name = "Registration")]
    public string? RegistrationNumber { get; set; }

    [Required(ErrorMessage = "Pick the locality.")]
    [StringLength(80)]
    public string Locality { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Defaults to false deliberately: consent must be ticked, never assumed. A post that
    /// omits this field grants nothing.
    /// </summary>
    [Display(Name = "WhatsApp reminders")]
    public bool WhatsAppOptIn { get; set; }
}
