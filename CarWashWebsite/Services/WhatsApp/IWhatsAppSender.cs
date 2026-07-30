namespace CarWashWebsite.Services.WhatsApp;

/// <summary>
/// A business-initiated WhatsApp message.
///
/// WhatsApp only permits free-form text within 24 hours of the customer's last
/// message. Reminders are business-initiated and fall outside that window, so they
/// must go out as a *pre-approved template* — hence <see cref="TemplateName"/> and
/// positional <see cref="Parameters"/>. <see cref="Body"/> is the same message
/// rendered for humans: it is what the stub logs, what admin sees, and what a
/// click-to-send link pre-fills.
/// </summary>
public record WhatsAppMessage
{
    public required string ToPhone { get; init; }
    public required string TemplateName { get; init; }
    public required IReadOnlyList<string> Parameters { get; init; }
    public required string Body { get; init; }
    public string LanguageCode { get; init; } = "en";
}

public record WhatsAppResult(bool Success, string? MessageId, string? Error)
{
    public static WhatsAppResult Ok(string? id) => new(true, id, null);
    public static WhatsAppResult Fail(string error) => new(false, null, error);
}

public interface IWhatsAppSender
{
    /// <summary>Human-readable name of the active provider, shown in admin.</summary>
    string ProviderName { get; }

    /// <summary>True when the provider can actually deliver messages.</summary>
    bool CanSend { get; }

    Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken ct = default);
}
