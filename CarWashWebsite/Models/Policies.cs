namespace CarWashWebsite.Models;

public static class Policies
{
    /// <summary>Staff management and anything destructive.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Day-to-day admin: dashboard, booking queue, exports.</summary>
    public const string StaffOrAdmin = "StaffOrAdmin";
}
