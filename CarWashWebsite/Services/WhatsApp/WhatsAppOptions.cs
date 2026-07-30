namespace CarWashWebsite.Services.WhatsApp;

public class WhatsAppOptions
{
    public const string Section = "WhatsApp";

    /// <summary>"Logging" (default, sends nothing) or "CloudApi".</summary>
    public string Provider { get; set; } = "Logging";

    /// <summary>Master switch. When false, reminders are still computed and logged but never dispatched.</summary>
    public bool Enabled { get; set; } = true;

    // ---- Meta Cloud API ----
    /// <summary>The WhatsApp Business phone number id from Meta (not the phone number itself).</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Permanent access token. Keep this in user-secrets, never in appsettings.</summary>
    public string? AccessToken { get; set; }

    public string GraphApiVersion { get; set; } = "v21.0";

    /// <summary>Default country code prefixed to 10-digit Indian numbers.</summary>
    public string DefaultCountryCode { get; set; } = "91";

    /// <summary>Template names registered and approved in the Meta console.</summary>
    public TemplateNames Templates { get; set; } = new();

    // ---- scheduling ----
    /// <summary>Hour of day (local, 24h) the daily reminder sweep runs.</summary>
    public int DailyRunHour { get; set; } = 10;

    /// <summary>No message is dispatched before this hour, local time.</summary>
    public int QuietHoursStart { get; set; } = 21;

    /// <summary>No message is dispatched after this hour, local time.</summary>
    public int QuietHoursEnd { get; set; } = 9;

    /// <summary>Send a "washes remaining" nudge this many days before the period ends.</summary>
    public int WashesRemainingLeadDays { get; set; } = 5;

    /// <summary>Send the renewal notice this many days before billing.</summary>
    public int RenewalLeadDays { get; set; } = 3;

    /// <summary>A subscriber with no wash for this many days gets a win-back.</summary>
    public int WinBackAfterDays { get; set; } = 42;

    /// <summary>Safety valve so a bug cannot message the whole customer base at once.</summary>
    public int MaxMessagesPerRun { get; set; } = 200;
}

public class TemplateNames
{
    public string WashesRemaining { get; set; } = "washes_remaining";
    public string Renewal { get; set; } = "plan_renewal";
    public string BookingTomorrow { get; set; } = "booking_tomorrow";
    public string WinBack { get; set; } = "winback_offer";
}
