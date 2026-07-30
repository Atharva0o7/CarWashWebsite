using System.ComponentModel.DataAnnotations;

namespace CarWashWebsite.Models;

/// <summary>Inbound payload from the /book form. Not an entity — maps onto <see cref="Booking"/>.</summary>
public class BookingRequest
{
    [Required(ErrorMessage = "Tell us your name.")]
    [StringLength(80, MinimumLength = 2)]
    [Display(Name = "Your name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "We need a number to confirm on.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    [Display(Name = "Mobile number")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "That email does not look right.")]
    [StringLength(160)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Pick your car's brand.")]
    [StringLength(80)]
    [Display(Name = "Brand")]
    public string CarBrand { get; set; } = "";

    [Required(ErrorMessage = "Pick your car's model.")]
    [StringLength(80)]
    [Display(Name = "Model")]
    public string CarModel { get; set; } = "";

    [Required(ErrorMessage = "Choose a service.")]
    [StringLength(60)]
    [Display(Name = "Service")]
    public string ServiceSlug { get; set; } = "";

    [Required(ErrorMessage = "Choose your area.")]
    [StringLength(80)]
    [Display(Name = "Locality")]
    public string Locality { get; set; } = "";

    [Required(ErrorMessage = "Pick a date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Preferred date")]
    public DateOnly PreferredDate { get; set; }

    [Required(ErrorMessage = "Pick a time slot.")]
    [StringLength(40)]
    [Display(Name = "Time slot")]
    public string PreferredSlot { get; set; } = "";

    [StringLength(400)]
    [Display(Name = "Anything we should know?")]
    public string? Notes { get; set; }
}

public class BookingResult
{
    public required string Reference { get; init; }
    public required string Message { get; init; }
    public int QuotedPrice { get; init; }
}
