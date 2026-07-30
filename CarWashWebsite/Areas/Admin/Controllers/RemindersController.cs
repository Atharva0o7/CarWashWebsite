using CarWashWebsite.Data;
using CarWashWebsite.Models;
using CarWashWebsite.Services;
using CarWashWebsite.Services.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/reminders")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class RemindersController(
    AppDbContext db,
    ReminderService reminders,
    IWhatsAppSender sender,
    IOptions<WhatsAppOptions> options) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(ReminderStatus? status, ReminderType? type, CancellationToken ct)
    {
        ViewData["Title"] = "Reminders — PitStop Admin";
        ViewData["ActiveNav"] = "reminders";

        var q = db.ReminderLogs.AsNoTracking().AsQueryable();
        if (status is { } s) q = q.Where(r => r.Status == s);
        if (type is { } t) q = q.Where(r => r.Type == t);

        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(ct);

        return View(new ReminderPageViewModel
        {
            Status = status,
            Type = type,
            Logs = rows,
            ProviderName = sender.ProviderName,
            CanSend = sender.CanSend,
            Options = options.Value,
            Counts = await db.ReminderLogs
                .GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct),
        });
    }

    /// <summary>Runs the sweep immediately, ignoring quiet hours so it is testable on demand.</summary>
    [HttpPost("run")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        var result = await reminders.RunAsync(ignoreQuietHours: true, ct);

        TempData["Flash"] = result.Total == 0
            ? "Nothing was due — everyone eligible has already been reminded."
            : $"{result.Total} queued · {result.Sent} sent · {result.Skipped} deferred · {result.Failed} failed.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Retries whatever is still Pending or Failed.</summary>
    [HttpPost("retry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(CancellationToken ct)
    {
        // Explicit human action, so quiet hours are bypassed deliberately.
        var result = await reminders.DispatchOutstandingAsync(ignoreQuietHours: true, ct);

        TempData["Flash"] = result.Total == 0
            ? "Nothing outstanding to retry."
            : $"Retried {result.Total}: {result.Sent} sent, {result.Failed} still failing.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Marks one as handled — used when staff sent it by hand via a click-to-send link.</summary>
    [HttpPost("{id:int}/sent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSent(int id, CancellationToken ct)
    {
        var log = await db.ReminderLogs.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (log is null)
        {
            return NotFound();
        }

        log.Status = ReminderStatus.Sent;
        log.SentAt = DateTimeOffset.UtcNow;
        log.ProviderMessageId = "manual";
        log.Error = null;
        await db.SaveChangesAsync(ct);

        TempData["Flash"] = $"Marked as sent to {log.CustomerName}.";
        return RedirectToAction(nameof(Index));
    }
}

public class ReminderPageViewModel
{
    public required ReminderStatus? Status { get; init; }
    public required ReminderType? Type { get; init; }
    public required List<ReminderLog> Logs { get; init; }
    public required string ProviderName { get; init; }
    public required bool CanSend { get; init; }
    public required WhatsAppOptions Options { get; init; }
    public required Dictionary<ReminderStatus, int> Counts { get; init; }

    public int CountOf(ReminderStatus status) => Counts.TryGetValue(status, out var n) ? n : 0;
}
