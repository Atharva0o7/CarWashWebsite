using System.ComponentModel.DataAnnotations.Schema;

namespace CarWashWebsite.Models;

/// <summary>Table: <c>testimonials</c>.</summary>
public class Testimonial
{
    public int Id { get; set; }
    public string Quote { get; set; } = "";
    public string Author { get; set; } = "";
    public string Car { get; set; } = "";
    public string Locality { get; set; } = "";
    public int Rating { get; set; } = 5;
    public int SortOrder { get; set; }

    [NotMapped]
    public string Initials =>
        string.Concat(Author.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2)
                            .Select(part => part[0]))
              .ToUpperInvariant();
}
