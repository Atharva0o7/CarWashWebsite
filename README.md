# PitStop Autocare

A car wash and detailing website for Pune, modelled on the GoMechanic city-page layout.
ASP.NET Core 10 MVC with Razor views, backed by local PostgreSQL via EF Core.

## Running it

1. **PostgreSQL must be running** on `localhost:5432`. (PostgreSQL 18 is installed as the
   `postgresql-x64-18` Windows service.)

2. **Set the connection password** — it lives in user-secrets, not in the repo:

   ```powershell
   cd "D:\L O C A L  R E P O\CarWashWebsite\CarWashWebsite"
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=carwash;Username=postgres;Password=YOURPASS"
   ```

3. **Run.** The `carwash` database, its schema and its seed content are all created
   automatically on first startup.

   ```powershell
   dotnet run
   ```

   Or just press F5 in Visual Studio.

## Project layout

| Path | What's in it |
| --- | --- |
| `Models/` | EF entities (`Service`, `WashPlan`, `Testimonial`, `FaqItem`, `CarBrand`, `Locality`, `Booking`) plus the `BookingRequest` form DTO and view models |
| `Data/AppDbContext.cs` | Mapping, constraints, and the snake_case naming convention |
| `Data/SeedData.cs` | Initial catalogue content — applied once per empty table |
| `Data/DbInitializer.cs` | Migrate + seed, called from `Program.cs` before the first request |
| `Services/CatalogService.cs` | Catalogue reads and the body-type pricing rules |
| `Services/BookingService.cs` | Booking creation, server-side pricing, reference generation |
| `Controllers/HomeController.cs` | The pages |
| `Controllers/BookingsController.cs` | JSON API: `POST /api/bookings`, `GET /api/bookings/{ref}`, `GET /api/quote` |
| `Views/` | Razor views; `Views/Shared/_Icons.cshtml` is an inline SVG sprite so there are no image requests |
| `wwwroot/css/site.css` | The entire stylesheet — design tokens, components, responsive rules |
| `wwwroot/js/site.js` | Progressive enhancement only; every page works with JS disabled |
| `db/schema.sql` | Generated idempotent SQL for the current migration, for inspection in psql/pgAdmin |

## Pages

- `/` — hero with live price picker, 12-service grid, how-it-works, offers carousel,
  wash plans, price table with body-type switcher, reviews, coverage, FAQ accordion
- `/services` and `/services/{slug}` — catalogue and detail pages
- `/plans` — subscription packages
- `/book` — booking form with live quote summary
- `/book/confirmed/{reference}` — confirmation

## Pricing model

Every service stores one hatchback base price. Other body types are derived from a
multiplier (Sedan ×1.15, SUV ×1.35, Luxury ×1.75) and rounded to the nearest ₹10.
The same rule runs in `CatalogService.PriceFor` and in `site.js`, and the booking price
is always recalculated server-side — the form's number is never trusted.

## Changing content

Edit rows directly in Postgres; `SeedData` only fills tables that are empty. To change
the schema, add a migration:

```powershell
dotnet ef migrations add SomeChange
```

It is applied on the next startup.
