using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CarWashWebsite.Services.WhatsApp;

/// <summary>
/// Meta WhatsApp Cloud API. Posts an approved template to the Graph API:
///   POST https://graph.facebook.com/{version}/{phoneNumberId}/messages
///
/// Requires a Meta Business account, a registered WhatsApp sender number, a permanent
/// access token, and each template approved in the Meta console before it will send.
/// </summary>
public class CloudApiWhatsAppSender(
    HttpClient http,
    IOptions<WhatsAppOptions> options,
    ILogger<CloudApiWhatsAppSender> logger) : IWhatsAppSender
{
    private readonly WhatsAppOptions _options = options.Value;

    public string ProviderName => "Meta WhatsApp Cloud API";

    public bool CanSend =>
        !string.IsNullOrWhiteSpace(_options.PhoneNumberId)
        && !string.IsNullOrWhiteSpace(_options.AccessToken);

    public async Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken ct = default)
    {
        if (!CanSend)
        {
            return WhatsAppResult.Fail(
                "WhatsApp:PhoneNumberId or WhatsApp:AccessToken is not configured.");
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = Normalise(message.ToPhone),
            type = "template",
            template = new
            {
                name = message.TemplateName,
                language = new { code = message.LanguageCode },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = message.Parameters
                            .Select(p => new { type = "text", text = p })
                            .ToArray(),
                    },
                },
            },
        };

        var url = $"https://graph.facebook.com/{_options.GraphApiVersion}/{_options.PhoneNumberId}/messages";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "WhatsApp send failed ({Status}) for {Phone}: {Body}",
                    (int)response.StatusCode, message.ToPhone, body);

                return WhatsAppResult.Fail($"HTTP {(int)response.StatusCode}: {Truncate(body, 400)}");
            }

            // { "messages": [ { "id": "wamid...." } ] }
            using var doc = JsonDocument.Parse(body);
            var id = doc.RootElement.TryGetProperty("messages", out var messages)
                     && messages.GetArrayLength() > 0
                     && messages[0].TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;

            return WhatsAppResult.Ok(id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "WhatsApp request to {Phone} did not complete.", message.ToPhone);
            return WhatsAppResult.Fail(ex.Message);
        }
    }

    /// <summary>Cloud API wants digits only, including country code and no leading +.</summary>
    private string Normalise(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length == 10
            ? _options.DefaultCountryCode + digits
            : digits;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
