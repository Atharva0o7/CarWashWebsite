using CarWashWebsite.Data;
using CarWashWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Services;

public class SubscriptionService(AppDbContext db, ILogger<SubscriptionService> logger)
{
    public Task<List<Subscription>> ListAsync(SubscriptionStatus? status, CancellationToken ct = default)
    {
        var q = db.Subscriptions.AsNoTracking().Include(s => s.Plan).AsQueryable();

        if (status is { } s)
        {
            q = q.Where(x => x.Status == s);
        }

        return q.OrderBy(x => x.Status).ThenBy(x => x.CurrentPeriodEnd).ToListAsync(ct);
    }

    public Task<Subscription?> FindAsync(int id, CancellationToken ct = default) =>
        db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Subscription> CreateAsync(Subscription input, CancellationToken ct = default)
    {
        var plan = await db.WashPlans.FirstOrDefaultAsync(p => p.Id == input.WashPlanId, ct)
            ?? throw new ArgumentException($"Unknown plan {input.WashPlanId}.", nameof(input));

        var start = input.StartDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : input.StartDate;

        input.Reference = await GenerateReferenceAsync(ct);
        input.MonthlyPrice = input.MonthlyPrice > 0 ? input.MonthlyPrice : plan.PricePerMonth;
        input.WashesIncluded = input.WashesIncluded > 0 ? input.WashesIncluded : plan.WashesIncluded;
        input.StartDate = start;
        input.CurrentPeriodStart = start;
        input.CurrentPeriodEnd = start.AddMonths(1).AddDays(-1);
        input.WashesUsedThisPeriod = 0;
        input.CreatedAt = DateTimeOffset.UtcNow;

        db.Subscriptions.Add(input);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Subscription {Reference}: {Plan} for {Customer} ({Car}) at ₹{Price}/month",
            input.Reference, plan.Name, input.CustomerName,
            $"{input.CarBrand} {input.CarModel}", input.MonthlyPrice);

        return input;
    }

    public async Task<bool> SetStatusAsync(int id, SubscriptionStatus status, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sub is null) return false;

        sub.Status = status;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetOptInAsync(int id, bool optIn, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sub is null) return false;

        sub.WhatsAppOptIn = optIn;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Records a wash against the plan. Rolls the period forward first if the current
    /// one has already ended, so allowances reset without a separate billing job.
    /// </summary>
    public async Task<bool> RecordWashAsync(int id, DateOnly on, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sub is null) return false;

        RollPeriodIfDue(sub, on);

        sub.WashesUsedThisPeriod++;
        sub.LastWashOn = on;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Advances any subscription whose period has lapsed. Called by the admin roll-over action.</summary>
    public async Task<int> RollLapsedPeriodsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var due = await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd < today)
            .ToListAsync(ct);

        foreach (var sub in due)
        {
            RollPeriodIfDue(sub, today);
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Rolled {Count} subscription period(s) forward.", due.Count);
        }

        return due.Count;
    }

    private static void RollPeriodIfDue(Subscription sub, DateOnly on)
    {
        // Loop rather than single-step, so a long-dormant subscription catches up.
        while (on > sub.CurrentPeriodEnd)
        {
            sub.CurrentPeriodStart = sub.CurrentPeriodEnd.AddDays(1);
            sub.CurrentPeriodEnd = sub.CurrentPeriodStart.AddMonths(1).AddDays(-1);
            sub.WashesUsedThisPeriod = 0;
        }
    }

    private async Task<string> GenerateReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"SUB-{DateTime.Now:yyMM}-{Random.Shared.Next(1000, 10000)}";
            if (!await db.Subscriptions.AnyAsync(s => s.Reference == candidate, ct))
            {
                return candidate;
            }
        }

        return $"SUB-{DateTime.Now:yyMMddHHmmss}";
    }
}
