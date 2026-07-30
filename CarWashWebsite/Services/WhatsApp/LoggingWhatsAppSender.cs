namespace CarWashWebsite.Services.WhatsApp;

/// <summary>
/// The default provider. Records exactly what would have been sent and reports
/// success, so the whole reminder pipeline is exercisable with no Meta account.
///
/// <see cref="CanSend"/> is false, which the admin UI uses to show click-to-send
/// links instead of pretending messages went out.
/// </summary>
public class LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger) : IWhatsAppSender
{
    public string ProviderName => "Logging (no messages are delivered)";

    public bool CanSend => false;

    public Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[WhatsApp:stub] → {Phone} using template {Template}\n{Body}",
            message.ToPhone, message.TemplateName, message.Body);

        return Task.FromResult(WhatsAppResult.Ok($"stub-{Guid.NewGuid():N}"[..16]));
    }
}
