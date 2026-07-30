using System.ComponentModel.DataAnnotations.Schema;

namespace CarWashWebsite.Models;

/// <summary>A single wash / detailing service shown in the services grid. Table: <c>services</c>.</summary>
public class Service
{
    public int Id { get; set; }

    /// <summary>URL segment, e.g. <c>/services/ceramic-coating</c>. Unique.</summary>
    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";

    /// <summary>Key of an inline SVG sprite symbol in <c>_Icons.cshtml</c> (without the <c>i-</c> prefix).</summary>
    public string Icon { get; set; } = "";

    /// <summary>Hatchback price in rupees. Other body types are derived via a multiplier.</summary>
    public int StartingPrice { get; set; }

    public int ListPrice { get; set; }
    public string Duration { get; set; } = "";
    public string Warranty { get; set; } = "";
    public bool IsNew { get; set; }
    public bool IsBestseller { get; set; }
    public int SortOrder { get; set; }

    public List<ServiceInclude> Includes { get; set; } = [];

    [NotMapped]
    public int SavingsPercent =>
        ListPrice > StartingPrice && ListPrice > 0
            ? (int)Math.Round((ListPrice - StartingPrice) / (double)ListPrice * 100)
            : 0;
}

/// <summary>One "what's included" bullet for a service. Table: <c>service_includes</c>.</summary>
public class ServiceInclude
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public string Text { get; set; } = "";
    public int SortOrder { get; set; }
}
