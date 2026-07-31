using CarWashWebsite.Data;
using CarWashWebsite.Infrastructure;
using CarWashWebsite.Models;
using CarWashWebsite.Services;
using CarWashWebsite.Services.WhatsApp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC (Razor views) + the JSON API controllers.
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Serialise enums as names, matching how they are stored in Postgres.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// OpenAPI document for the JSON API controllers, exposed in development only.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' is missing. Set ConnectionStrings:Postgres in appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ---------------------------------------------------------------- identity
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;

        // Lockout disabled by request: a wrong password never locks the account, so
        // staff are never shut out mid-shift. The trade-off is that there is no longer
        // any brute-force throttle — password strength is the only guard. Re-enable by
        // setting AllowedForNewUsers = true and passing lockoutOnFailure: true at sign-in.
        options.Lockout.AllowedForNewUsers = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "pitstop.admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/admin/denied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.StaffOrAdmin, p => p.RequireRole(Roles.Admin, Roles.Staff));
});

builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<BookingAdminService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<ReminderService>();

// ---------------------------------------------------------------- whatsapp
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.Section));

var whatsApp = builder.Configuration.GetSection(WhatsAppOptions.Section).Get<WhatsAppOptions>()
               ?? new WhatsAppOptions();

if (string.Equals(whatsApp.Provider, "CloudApi", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IWhatsAppSender, CloudApiWhatsAppSender>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);
    });
}
else
{
    // Default: record what would be sent, deliver nothing. No credentials needed.
    builder.Services.AddScoped<IWhatsAppSender, LoggingWhatsAppSender>();
}

builder.Services.AddHostedService<ReminderBackgroundService>();

var adminPortOptions = new AdminPortOptions
{
    Ports = builder.Configuration.GetSection("Admin:Ports").Get<int[]>() ?? [5300, 7300],
};

var app = builder.Build();

// Create/upgrade the schema, seed the catalogue, and ensure an admin login exists.
await DbInitializer.InitialiseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Sits after routing (so the path is known) and before authentication, so a blocked
// admin request never reaches the auth handlers at all.
app.UseAdminPortIsolation(adminPortOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
