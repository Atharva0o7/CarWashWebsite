using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations, then seeds any catalogue table that is still empty.
    /// Safe to run on every startup — seeding is per-table and idempotent.
    /// </summary>
    public static async Task InitialiseAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
            logger.LogInformation("Database up to date; nothing to seed.");
            return;
        }

        var rows = await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Tables} ({Rows} rows).", string.Join(", ", seeded), rows);
    }
}
