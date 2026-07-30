using System.Text;
using CarWashWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceInclude> ServiceIncludes => Set<ServiceInclude>();
    public DbSet<WashPlan> WashPlans => Set<WashPlan>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<FaqItem> Faqs => Set<FaqItem>();
    public DbSet<CarBrand> CarBrands => Set<CarBrand>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<Locality> Localities => Set<Locality>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Service>(e =>
        {
            e.ToTable("services");
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(60).IsRequired();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Tagline).HasMaxLength(200);
            e.Property(x => x.Icon).HasMaxLength(40);
            e.Property(x => x.Duration).HasMaxLength(60);
            e.Property(x => x.Warranty).HasMaxLength(120);
            e.HasMany(x => x.Includes)
             .WithOne(x => x.Service!)
             .HasForeignKey(x => x.ServiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ServiceInclude>(e =>
        {
            e.ToTable("service_includes");
            e.Property(x => x.Text).HasMaxLength(200).IsRequired();
        });

        b.Entity<WashPlan>(e =>
        {
            e.ToTable("wash_plans");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Blurb).HasMaxLength(200);
            e.Property(x => x.WashCount).HasMaxLength(120);
            e.HasMany(x => x.Features)
             .WithOne(x => x.WashPlan!)
             .HasForeignKey(x => x.WashPlanId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PlanFeature>(e =>
        {
            e.ToTable("plan_features");
            e.Property(x => x.Text).HasMaxLength(200).IsRequired();
        });

        b.Entity<Testimonial>(e =>
        {
            e.ToTable("testimonials");
            e.Property(x => x.Quote).HasMaxLength(400).IsRequired();
            e.Property(x => x.Author).HasMaxLength(80).IsRequired();
            e.Property(x => x.Car).HasMaxLength(80);
            e.Property(x => x.Locality).HasMaxLength(80);
            e.ToTable(t => t.HasCheckConstraint("ck_testimonials_rating", "rating BETWEEN 1 AND 5"));
        });

        b.Entity<FaqItem>(e =>
        {
            e.ToTable("faqs");
            e.Property(x => x.Question).HasMaxLength(200).IsRequired();
            e.Property(x => x.Answer).HasMaxLength(1200).IsRequired();
        });

        b.Entity<CarBrand>(e =>
        {
            e.ToTable("car_brands");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.HasMany(x => x.Models)
             .WithOne(x => x.Brand!)
             .HasForeignKey(x => x.CarBrandId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CarModel>(e =>
        {
            e.ToTable("car_models");
            e.HasIndex(x => new { x.CarBrandId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.BodyType).HasMaxLength(20).IsRequired();
        });

        b.Entity<Locality>(e =>
        {
            e.ToTable("localities");
            e.HasIndex(x => new { x.City, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.City).HasMaxLength(80).IsRequired();
        });

        b.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => x.Phone);
            e.HasIndex(x => new { x.PreferredDate, x.Status });
            e.Property(x => x.Reference).HasMaxLength(24).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(80).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(15).IsRequired();
            e.Property(x => x.Email).HasMaxLength(160);
            e.Property(x => x.CarBrand).HasMaxLength(80).IsRequired();
            e.Property(x => x.CarModel).HasMaxLength(80).IsRequired();
            e.Property(x => x.BodyType).HasMaxLength(20);
            e.Property(x => x.Locality).HasMaxLength(80).IsRequired();
            e.Property(x => x.PreferredSlot).HasMaxLength(40).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(400);
            // Stored as text ("Pending", "Confirmed", …) so the table stays readable in psql.
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.Service)
             .WithMany()
             .HasForeignKey(x => x.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        ApplySnakeCaseColumnNames(b);
    }

    /// <summary>
    /// Postgres folds unquoted identifiers to lower case, so PascalCase columns would need
    /// quoting in every hand-written query. Rename columns, keys and indexes to snake_case
    /// once here rather than annotating ~60 properties.
    /// </summary>
    private static void ApplySnakeCaseColumnNames(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                // Insert a separator at lower→upper and at the end of an acronym run (e.g. "IDValue").
                var prev = i > 0 ? name[i - 1] : '\0';
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                var boundary = i > 0 && prev != '_'
                    && (!char.IsUpper(prev) || (char.IsUpper(prev) && char.IsLower(next)));

                if (boundary)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
