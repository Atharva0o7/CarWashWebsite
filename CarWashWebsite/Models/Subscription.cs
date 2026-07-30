namespace CarWashWebsite.Models;

/// <summary>A customer on a monthly wash plan. Table: <c>subscriptions</c>.</summary>
public class Subscription
{
    public int Id { get; set; }

    /// <summary>Short human-readable reference, e.g. <c>SUB-2607-4821</c>. Unique.</summary>
    public string Reference { get; set; } = "";

    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }

    public string CarBrand { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string BodyType { get; set; } = "";

    /// <summary>Registration number, so a plan is tied to one car.</summary>
    public string? RegistrationNumber { get; set; }

    public int WashPlanId { get; set; }
    public WashPlan? Plan { get; set; }

    public string Locality { get; set; } = "";

    /// <summary>Price actually agreed, in rupees — kept so later plan price changes don't rewrite history.</summary>
    public int MonthlyPrice { get; set; }

    /// <summary>Washes the customer gets each period, snapshotted at signup.</summary>
    public int WashesIncluded { get; set; }

    public int WashesUsedThisPeriod { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly CurrentPeriodStart { get; set; }
    public DateOnly CurrentPeriodEnd { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>Last time this customer actually had a wash — drives win-back reminders.</summary>
    public DateOnly? LastWashOn { get; set; }

    /// <summary>Opt-out flag. No reminder of any kind is sent when this is false.</summary>
    public bool WhatsAppOptIn { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public int WashesRemaining => Math.Max(0, WashesIncluded - WashesUsedThisPeriod);

    /// <summary>Next billing date is the day after the current period ends.</summary>
    public DateOnly RenewsOn => CurrentPeriodEnd.AddDays(1);
}

public enum SubscriptionStatus
{
    Active = 0,
    Paused = 1,
    Cancelled = 2,
    Expired = 3,
}
