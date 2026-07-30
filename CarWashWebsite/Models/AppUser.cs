using Microsoft.AspNetCore.Identity;

namespace CarWashWebsite.Models;

/// <summary>A staff login. Table: <c>users</c>.</summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}

/// <summary>Role names, kept in one place so controllers and seeding agree.</summary>
public static class Roles
{
    /// <summary>Full access, including staff management.</summary>
    public const string Admin = "Admin";

    /// <summary>Can view the dashboard and work the booking queue, but not manage staff.</summary>
    public const string Staff = "Staff";

    public static readonly string[] All = [Admin, Staff];
}
