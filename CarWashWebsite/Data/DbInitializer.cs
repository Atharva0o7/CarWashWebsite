using CarWashWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations, then seeds any catalogue table that is still empty
    /// and makes sure at least one admin login exists.
    /// Safe to run on every startup — seeding is per-table and idempotent.
    /// </summary>
    public static async Task InitialiseAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                          .CreateLogger(typeof(DbInitializer));

        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (Npgsql.NpgsqlException ex)
        {
            // The usual causes are a stopped server or a wrong password — say so plainly
            // instead of dumping a connector stack trace on someone's first run.
            logger.LogCritical(
                ex,
                "Could not reach PostgreSQL. Check that the server is running on the host/port in "
                + "ConnectionStrings:Postgres and that the username and password are correct. "
                + "Underlying error: {Error}",
                ex.Message);
            throw;
        }

        var seeded = new List<string>();

        if (!await db.Services.AnyAsync(ct))
        {
            db.Services.AddRange(SeedData.Services());
            seeded.Add("services");
        }

        if (!await db.WashPlans.AnyAsync(ct))
        {
            db.WashPlans.AddRange(SeedData.Plans());
            seeded.Add("wash_plans");
        }

        if (!await db.Testimonials.AnyAsync(ct))
        {
            db.Testimonials.AddRange(SeedData.Testimonials());
            seeded.Add("testimonials");
        }

        if (!await db.Faqs.AnyAsync(ct))
        {
            db.Faqs.AddRange(SeedData.Faqs());
            seeded.Add("faqs");
        }

        if (!await db.Localities.AnyAsync(ct))
        {
            db.Localities.AddRange(SeedData.Localities());
            seeded.Add("localities");
        }

        if (!await db.CarBrands.AnyAsync(ct))
        {
            db.CarBrands.AddRange(SeedData.CarBrands());
            seeded.Add("car_brands");
        }

        if (seeded.Count == 0)
        {
            logger.LogInformation("Catalogue up to date; nothing to seed.");
        }
        else
        {
            var rows = await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Tables} ({Rows} rows).", string.Join(", ", seeded), rows);
        }

        await SeedIdentityAsync(scope.ServiceProvider, config, logger);
    }

    /// <summary>
    /// Ensures the two roles exist and that there is at least one Admin to log in with.
    /// The bootstrap password comes from configuration (put it in user-secrets) and is
    /// only ever used to create the very first account.
    /// </summary>
    private static async Task SeedIdentityAsync(
        IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role {Role}.", role);
            }
        }

        // Only bootstrap when there is no admin at all — never overwrite a real account.
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0)
        {
            return;
        }

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No admin account exists and Admin:Email / Admin:Password are not configured, so "
                + "the admin area cannot be signed into. Set them with:\n"
                + "  dotnet user-secrets set \"Admin:Email\" \"you@example.com\"\n"
                + "  dotnet user-secrets set \"Admin:Password\" \"<10+ chars, upper, lower, digit>\"");
            return;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = config["Admin:FullName"] ?? "Administrator",
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError(
                "Could not create the bootstrap admin: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.Admin);
        logger.LogInformation("Created bootstrap admin {Email}.", email);
    }
}
