using CarWashWebsite.Services.WhatsApp;
using Microsoft.Extensions.Options;

namespace CarWashWebsite.Services;

/// <summary>
/// Runs the reminder sweep once a day at <see cref="WhatsAppOptions.DailyRunHour"/>,
/// and retries anything left outstanding every hour in between (which is how messages
/// deferred by quiet hours eventually go out).
///
/// This is a single-process timer, which is right for one server. If this ever runs on
/// more than one instance, move the trigger to a real scheduler or add a database lock —
/// otherwise every instance sweeps. The dedupe key would still prevent double-sends, but
/// the work would be duplicated.
/// </summary>
public class ReminderBackgroundService(
    IServiceProvider services,
    IOptions<WhatsAppOptions> options,
    ILogger<ReminderBackgroundService> logger) : BackgroundService
{
    private readonly WhatsAppOptions _options = options.Value;
    private DateOnly _lastSweep = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Reminder scheduler started — daily sweep at {Hour:00}:00, quiet hours {Start:00}:00–{End:00}:00.",
            _options.DailyRunHour, _options.QuietHoursStart, _options.QuietHoursEnd);

        // Let the app finish starting before touching the database.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a bad run kill the loop — tomorrow's sweep should still happen.
                logger.LogError(ex, "Reminder sweep failed; will retry on the next tick.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Reminder scheduler stopped.");
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        using var scope = services.CreateScope();
        var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();

        if (_lastSweep < today && now.Hour >= _options.DailyRunHour)
        {
            _lastSweep = today;
            var result = await reminders.RunAsync(ct: ct);

            if (result.Total > 0)
            {
                logger.LogInformation(
                    "Daily sweep: {Sent} sent, {Skipped} deferred, {Failed} failed.",
                    result.Sent, result.Skipped, result.Failed);
            }

            return;
        }

        // Between sweeps, push out anything still pending or previously failed.
        // Quiet hours are respected here, so overnight deferrals wait until morning.
        await reminders.DispatchOutstandingAsync(ct: ct);
    }
}
