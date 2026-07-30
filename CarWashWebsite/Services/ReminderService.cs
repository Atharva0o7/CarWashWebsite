using CarWashWebsite.Data;
using CarWashWebsite.Models;
using CarWashWebsite.Services.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWashWebsite.Services;

public record ReminderRunResult(int Queued, int Sent, int Skipped, int Failed)
{
    public static readonly ReminderRunResult Empty = new(0, 0, 0, 0);
    public int Total => Queued;
}

/// <summary>
/// Decides which reminders are due, records them, and dispatches them.
///
/// Every reminder gets a <see cref="ReminderLog.DedupeKey"/> that encodes the subject
/// and the period it belongs to. A unique index on that column means running the sweep
/// twice — or running it manually right after the nightly job — cannot double-message
/// anyone. That guarantee lives in the database, not in this code's control flow.
/// </summary>
public class ReminderService(
    AppDbContext db,
    IWhatsAppSender sender,
    IOptions<WhatsAppOptions> options,
    ILogger<ReminderService> logger)
{
    private readonly WhatsAppOptions _options = options.Value;

    /// <summary>Builds and dispatches every due reminder.</summary>
    public async Task<ReminderRunResult> RunAsync(
        bool ignoreQuietHours = false, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var candidates = new List<ReminderLog>();
        candidates.AddRange(await WashesRemainingAsync(today, ct));
        candidates.AddRange(await RenewalAsync(today, ct));
        candidates.AddRange(await BookingTomorrowAsync(today, ct));
        candidates.AddRange(await WinBackAsync(today, ct));

        if (candidates.Count == 0)
        {
            logger.LogInformation("Reminder sweep: nothing due.");
            return ReminderRunResult.Empty;
        }

        // Drop anything already recorded — the unique index is the real guard, but
        // filtering first avoids a wall of constraint violations.
        var keys = candidates.Select(c => c.DedupeKey).ToList();
        var existing = await db.ReminderLogs
            .Where(r => keys.Contains(r.DedupeKey))
            .Select(r => r.DedupeKey)
            .ToListAsync(ct);

        var fresh = candidates
            .Where(c => !existing.Contains(c.DedupeKey))
            .DistinctBy(c => c.DedupeKey)
            .Take(_options.MaxMessagesPerRun)
            .ToList();

        if (fresh.Count == 0)
        {
            logger.LogInformation("Reminder sweep: {Count} due, all already sent.", candidates.Count);
            return ReminderRunResult.Empty;
        }

        db.ReminderLogs.AddRange(fresh);
        await db.SaveChangesAsync(ct);

        var sent = 0;
        var skipped = 0;
        var failed = 0;
        var quiet = !ignoreQuietHours && InQuietHours(DateTime.Now);

        foreach (var log in fresh)
        {
            if (!_options.Enabled)
            {
                log.Status = ReminderStatus.Skipped;
                log.Error = "WhatsApp:Enabled is false.";
                skipped++;
                continue;
            }

            if (quiet)
            {
                // Leave it Pending — the next in-hours run picks it up.
                skipped++;
                continue;
            }

            var result = await sender.SendAsync(new WhatsAppMessage
            {
                ToPhone = log.Phone,
                TemplateName = log.TemplateName,
                Parameters = ParametersFor(log),
                Body = log.Body,
            }, ct);

            if (result.Success)
            {
                log.Status = ReminderStatus.Sent;
                log.ProviderMessageId = result.MessageId;
                log.SentAt = DateTimeOffset.UtcNow;
                sent++;
            }
            else
            {
                log.Status = ReminderStatus.Failed;
                log.Error = result.Error;
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Reminder sweep via {Provider}: {Queued} queued, {Sent} sent, {Skipped} skipped, {Failed} failed.",
            sender.ProviderName, fresh.Count, sent, skipped, failed);

        return new ReminderRunResult(fresh.Count, sent, skipped, failed);
    }

    /// <summary>
    /// Retries the ones left Pending or Failed. Respects quiet hours by default — the
    /// scheduler calls this every half hour, so without that check a message deferred
    /// at 21:00 would simply go out at 21:30 and quiet hours would mean nothing.
    /// The admin "retry" button passes <c>true</c>, since that is a deliberate human act.
    /// </summary>
    public async Task<ReminderRunResult> DispatchOutstandingAsync(
        bool ignoreQuietHours = false, CancellationToken ct = default)
    {
        if (!_options.Enabled || (!ignoreQuietHours && InQuietHours(DateTime.Now)))
        {
            return ReminderRunResult.Empty;
        }

        var outstanding = await db.ReminderLogs
            .Where(r => r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Failed)
            .OrderBy(r => r.CreatedAt)
            .Take(_options.MaxMessagesPerRun)
            .ToListAsync(ct);

        if (outstanding.Count == 0)
        {
            return ReminderRunResult.Empty;
        }

        var sent = 0;
        var failed = 0;

        foreach (var log in outstanding)
        {
            var result = await sender.SendAsync(new WhatsAppMessage
            {
                ToPhone = log.Phone,
                TemplateName = log.TemplateName,
                Parameters = ParametersFor(log),
                Body = log.Body,
            }, ct);

            if (result.Success)
            {
                log.Status = ReminderStatus.Sent;
                log.ProviderMessageId = result.MessageId;
                log.SentAt = DateTimeOffset.UtcNow;
                log.Error = null;
                sent++;
            }
            else
            {
                log.Status = ReminderStatus.Failed;
                log.Error = result.Error;
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new ReminderRunResult(outstanding.Count, sent, 0, failed);
    }

    // ------------------------------------------------------------------ rules

    /// <summary>Active subscribers with unused washes as the period runs out.</summary>
    private async Task<List<ReminderLog>> WashesRemainingAsync(DateOnly today, CancellationToken ct)
    {
        var cutoff = today.AddDays(_options.WashesRemainingLeadDays);

        var subs = await Messageable()
            .Where(s => s.CurrentPeriodEnd >= today
                     && s.CurrentPeriodEnd <= cutoff
                     && s.WashesUsedThisPeriod < s.WashesIncluded)
            .ToListAsync(ct);

        return [.. subs.Select(s => new ReminderLog
        {
            Type = ReminderType.WashesRemaining,
            DedupeKey = $"washes:{s.Id}:{s.CurrentPeriodEnd:yyyyMMdd}",
            Phone = s.Phone,
            CustomerName = s.CustomerName,
            SubscriptionId = s.Id,
            TemplateName = _options.Templates.WashesRemaining,
            TemplateParamsJson = Params(
                FirstName(s.CustomerName),
                s.Plan?.Name ?? "your plan",
                s.WashesRemaining.ToString(),
                s.CurrentPeriodEnd.ToString("d MMM")),
            Body = $"Hi {FirstName(s.CustomerName)}, your {s.Plan?.Name} plan has "
                 + $"{s.WashesRemaining} wash{(s.WashesRemaining == 1 ? "" : "es")} left, "
                 + $"expiring {s.CurrentPeriodEnd:d MMM}. Reply BOOK and we will pick the "
                 + $"{s.CarModel} up. — PitStop Autocare",
            CreatedAt = DateTimeOffset.UtcNow,
        })];
    }

    /// <summary>Active subscribers billing in a few days.</summary>
    private async Task<List<ReminderLog>> RenewalAsync(DateOnly today, CancellationToken ct)
    {
        var target = today.AddDays(_options.RenewalLeadDays);

        var subs = await Messageable()
            .Where(s => s.CurrentPeriodEnd == target.AddDays(-1))
            .ToListAsync(ct);

        return [.. subs.Select(s => new ReminderLog
        {
            Type = ReminderType.Renewal,
            DedupeKey = $"renewal:{s.Id}:{s.RenewsOn:yyyyMMdd}",
            Phone = s.Phone,
            CustomerName = s.CustomerName,
            SubscriptionId = s.Id,
            TemplateName = _options.Templates.Renewal,
            TemplateParamsJson = Params(
                FirstName(s.CustomerName),
                s.Plan?.Name ?? "your plan",
                $"₹{s.MonthlyPrice:N0}",
                s.RenewsOn.ToString("d MMM")),
            Body = $"Hi {FirstName(s.CustomerName)}, your {s.Plan?.Name} plan renews on "
                 + $"{s.RenewsOn:d MMM} for ₹{s.MonthlyPrice:N0}. Reply PAUSE to skip a month "
                 + $"or STOP to cancel — no charge either way. — PitStop Autocare",
            CreatedAt = DateTimeOffset.UtcNow,
        })];
    }

    /// <summary>Anyone — subscriber or not — with a job tomorrow.</summary>
    private async Task<List<ReminderLog>> BookingTomorrowAsync(DateOnly today, CancellationToken ct)
    {
        var tomorrow = today.AddDays(1);

        var bookings = await db.Bookings
            .Include(b => b.Service)
            .Where(b => b.PreferredDate == tomorrow
                     && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync(ct);

        return [.. bookings.Select(b => new ReminderLog
        {
            Type = ReminderType.BookingTomorrow,
            DedupeKey = $"booking:{b.Id}:{b.PreferredDate:yyyyMMdd}",
            Phone = b.Phone,
            CustomerName = b.CustomerName,
            BookingId = b.Id,
            TemplateName = _options.Templates.BookingTomorrow,
            TemplateParamsJson = Params(
                FirstName(b.CustomerName),
                b.Service?.Name ?? "your wash",
                b.PreferredSlot,
                b.Locality),
            Body = $"Hi {FirstName(b.CustomerName)}, your {b.Service?.Name} is tomorrow, "
                 + $"{b.PreferredSlot}, in {b.Locality}. Reply R to reschedule. "
                 + $"Reference {b.Reference}. — PitStop Autocare",
            CreatedAt = DateTimeOffset.UtcNow,
        })];
    }

    /// <summary>Subscribers who have gone quiet.</summary>
    private async Task<List<ReminderLog>> WinBackAsync(DateOnly today, CancellationToken ct)
    {
        var stale = today.AddDays(-_options.WinBackAfterDays);

        var subs = await Messageable()
            .Where(s => s.LastWashOn == null ? s.StartDate <= stale : s.LastWashOn <= stale)
            .ToListAsync(ct);

        return [.. subs.Select(s =>
        {
            var since = s.LastWashOn ?? s.StartDate;
            var weeks = Math.Max(1, (today.DayNumber - since.DayNumber) / 7);

            return new ReminderLog
            {
                Type = ReminderType.WinBack,
                DedupeKey = $"winback:{s.Id}:{since:yyyyMMdd}",
                Phone = s.Phone,
                CustomerName = s.CustomerName,
                SubscriptionId = s.Id,
                TemplateName = _options.Templates.WinBack,
                TemplateParamsJson = Params(
                    FirstName(s.CustomerName),
                    s.CarModel,
                    s.Locality),
                Body = $"Hi {FirstName(s.CustomerName)}, we have not seen the {s.CarModel} in "
                     + $"{weeks} weeks and your plan is still running. Reply BOOK and we will "
                     + $"collect it from {s.Locality} — pickup is on us. — PitStop Autocare",
                CreatedAt = DateTimeOffset.UtcNow,
            };
        })];
    }

    /// <summary>Active subscribers who have not opted out. Every rule starts here.</summary>
    private IQueryable<Subscription> Messageable() =>
        db.Subscriptions
          .Include(s => s.Plan)
          .Where(s => s.Status == SubscriptionStatus.Active && s.WhatsAppOptIn);

    // ------------------------------------------------------------------ helpers

    /// <summary>Reads back the parameters frozen when the reminder was created.</summary>
    private static IReadOnlyList<string> ParametersFor(ReminderLog log)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(log.TemplateParamsJson)
                   ?? [FirstName(log.CustomerName)];
        }
        catch (System.Text.Json.JsonException)
        {
            return [FirstName(log.CustomerName)];
        }
    }

    private static string Params(params string[] values) =>
        System.Text.Json.JsonSerializer.Serialize(values);

    private static string FirstName(string full) =>
        full.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? full;

    /// <summary>
    /// Quiet hours wrap past midnight (e.g. 21:00 → 09:00), so the comparison differs
    /// depending on whether the window crosses the day boundary.
    /// </summary>
    private bool InQuietHours(DateTime now)
    {
        var hour = now.Hour;
        var start = _options.QuietHoursStart;
        var end = _options.QuietHoursEnd;

        return start > end
            ? hour >= start || hour < end
            : hour >= start && hour < end;
    }
}
