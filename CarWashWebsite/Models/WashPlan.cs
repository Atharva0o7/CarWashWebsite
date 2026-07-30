namespace CarWashWebsite.Models;

/// <summary>A recurring subscription package. Table: <c>wash_plans</c>.</summary>
public class WashPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Blurb { get; set; } = "";
    public int PricePerMonth { get; set; }
    public int ListPricePerMonth { get; set; }
    public string WashCount { get; set; } = "";
    public bool IsPopular { get; set; }
    public int SortOrder { get; set; }

    public List<PlanFeature> Features { get; set; } = [];
}

/// <summary>One feature bullet on a plan card. Table: <c>plan_features</c>.</summary>
public class PlanFeature
{
    public int Id { get; set; }
    public int WashPlanId { get; set; }
    public WashPlan? WashPlan { get; set; }
    public string Text { get; set; } = "";
    public int SortOrder { get; set; }
}
