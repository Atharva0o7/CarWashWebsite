namespace CarWashWebsite.Models;

/// <summary>A customer booking persisted from the /book form. Table: <c>bookings</c>.</summary>
public class Booking
{
    public int Id { get; set; }

    /// <summary>Short human-readable reference, e.g. <c>PIT-260730-4821</c>. Unique.</summary>
    public string Reference { get; set; } = "";

    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }

    public string CarBrand { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string BodyType { get; set; } = "";

    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    public string Locality { get; set; } = "";
    public DateOnly PreferredDate { get; set; }
    public string PreferredSlot { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>Price quoted at booking time, in rupees — kept so later price changes don't rewrite history.</summary>
    public int QuotedPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
