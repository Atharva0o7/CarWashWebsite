namespace CarWashWebsite.Models;

/// <summary>Table: <c>faqs</c>.</summary>
public class FaqItem
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
}
