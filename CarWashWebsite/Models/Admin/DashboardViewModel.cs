namespace CarWashWebsite.Models.Admin;

public class DashboardViewModel
{
    public required DateOnly From { get; init; }
    public required DateOnly To { get; init; }

    // Headline counters.
    public int JobsToday { get; init; }
    public int JobsThisWeek { get; init; }
    public int PendingCount { get; init; }
    public int TotalBookings { get; init; }

    public long RevenueBooked { get; init; }
    public long RevenueCompleted { get; init; }
    public int AverageTicket { get; init; }
    public double CancellationRate { get; init; }

    public required List<StatusSlice> ByStatus { get; init; }
    public required List<TrendPoint> Trend { get; init; }
    public required List<NamedCount> TopServices { get; init; }
    public required List<NamedCount> TopLocalities { get; init; }
    public required List<NamedCount> ByBodyType { get; init; }
    public required List<NamedCount> BySlot { get; init; }
    public required List<Booking> UpcomingJobs { get; init; }

    /// <summary>Largest bar value, so the templates can size bars without re-scanning.</summary>
    public static int Peak(IEnumerable<NamedCount> rows) =>
        rows.Select(r => r.Count).DefaultIfEmpty(0).Max();
}

public record StatusSlice(BookingStatus Status, int Count, long Revenue);

public record TrendPoint(DateOnly Date, int Count, long Revenue);

public record NamedCount(string Name, int Count, long Revenue);
