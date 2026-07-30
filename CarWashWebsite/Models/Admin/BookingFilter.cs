namespace CarWashWebsite.Models.Admin;

/// <summary>Query-string filter for the bookings queue. Also drives the Excel export.</summary>
public class BookingFilter
{
    public string? Q { get; set; }
    public BookingStatus? Status { get; set; }
    public string? Locality { get; set; }
    public string? ServiceSlug { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public bool IsFiltered =>
        !string.IsNullOrWhiteSpace(Q) || Status is not null
        || !string.IsNullOrWhiteSpace(Locality) || !string.IsNullOrWhiteSpace(ServiceSlug)
        || From is not null || To is not null;

    /// <summary>Rebuilds the query string, swapping in a new page number.</summary>
    public Dictionary<string, string?> ToRouteValues(int? page = null)
    {
        var values = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(Q)) values["q"] = Q;
        if (Status is not null) values["status"] = Status.ToString();
        if (!string.IsNullOrWhiteSpace(Locality)) values["locality"] = Locality;
        if (!string.IsNullOrWhiteSpace(ServiceSlug)) values["serviceSlug"] = ServiceSlug;
        if (From is not null) values["from"] = From.Value.ToString("yyyy-MM-dd");
        if (To is not null) values["to"] = To.Value.ToString("yyyy-MM-dd");
        if ((page ?? Page) > 1) values["page"] = (page ?? Page).ToString();
        return values;
    }
}

public class BookingListViewModel
{
    public required BookingFilter Filter { get; init; }
    public required List<Booking> Bookings { get; init; }
    public required List<string> Localities { get; init; }
    public required List<Service> Services { get; init; }

    public int TotalCount { get; init; }
    public long TotalValue { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(1, Filter.PageSize));
}
