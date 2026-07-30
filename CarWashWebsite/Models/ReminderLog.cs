namespace CarWashWebsite.Models;

/// <summary>
/// One reminder we decided to send. Written before dispatch, so a crash mid-send
/// never silently loses the record. Table: <c>reminder_logs</c>.
/// </summary>
public class ReminderLog
{
    public int Id { get; set; }

    public ReminderType Type { get; set; }

    /// <summary>
    /// Stable key that identifies "this reminder, for this subject, for this period".
    /// A unique index on it is what stops a customer being messaged twice.
    /// </summary>
    public string DedupeKey { get; set; } = "";

    public string Phone { get; set; } = "";
    public string CustomerName { get; set; } = "";

    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>Cloud API template name used (or that would be used).</summary>
    public string TemplateName { get; set; } = "";

    /// <summary>
    /// Positional template parameters, JSON-encoded, frozen when the reminder was
    /// decided. Stored rather than recomputed so a retry sends the same message even
    /// if the underlying plan or booking has since changed.
    /// </summary>
    public string TemplateParamsJson { get; set; } = "[]";

    /// <summary>The rendered human-readable text — what the customer sees, and what a
    /// click-to-send link pre-fills.</summary>
    public string Body { get; set; } = "";

    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    /// <summary>Provider message id, when one comes back.</summary>
    public string? ProviderMessageId { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public enum ReminderType
{
    /// <summary>"You have N washes left this month."</summary>
    WashesRemaining = 0,

    /// <summary>"Your plan renews on the 5th for ₹1,699."</summary>
    Renewal = 1,

    /// <summary>"Your wash is tomorrow at 10:00."</summary>
    BookingTomorrow = 2,

    /// <summary>"We haven't seen your car in a while."</summary>
    WinBack = 3,
}

public enum ReminderStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,

    /// <summary>Not sent on purpose — opted out, quiet hours, or no provider configured.</summary>
    Skipped = 3,
}
