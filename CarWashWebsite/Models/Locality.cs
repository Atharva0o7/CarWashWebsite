namespace CarWashWebsite.Models;

/// <summary>A serviceable area within the city. Table: <c>localities</c>.</summary>
public class Locality
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "Pune";

    /// <summary>False for areas we advertise but cannot yet reach.</summary>
    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
