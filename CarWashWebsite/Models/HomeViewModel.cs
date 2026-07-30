namespace CarWashWebsite.Models;

public class HomeViewModel
{
    public required string City { get; init; }
    public required List<Service> Services { get; init; }
    public required List<Service> FeaturedOffers { get; init; }
    public required List<WashPlan> Plans { get; init; }
    public required List<Testimonial> Testimonials { get; init; }
    public required List<FaqItem> Faqs { get; init; }
    public required List<CarBrand> Brands { get; init; }
    public required List<string> Localities { get; init; }
}

/// <summary>Everything the /book page needs to render its dropdowns.</summary>
public class BookingPageViewModel
{
    public required BookingRequest Form { get; init; }
    public required List<CarBrand> Brands { get; init; }
    public required List<Service> Services { get; init; }
    public required List<string> Localities { get; init; }
    public required List<string> TimeSlots { get; init; }
}
