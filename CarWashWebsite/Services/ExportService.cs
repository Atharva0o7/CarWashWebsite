using CarWashWebsite.Models;
using CarWashWebsite.Models.Admin;
using Microsoft.EntityFrameworkCore;
using static CarWashWebsite.Services.XlsxWriter;

namespace CarWashWebsite.Services;

/// <summary>Builds the admin Excel downloads.</summary>
public class ExportService(BookingAdminService bookings, AnalyticsService analytics)
{
    /// <summary>
    /// Bookings matching the current filter, plus summary sheets so the workbook is
    /// useful on its own rather than just a raw dump.
    /// </summary>
    public async Task<(byte[] Content, string FileName)> BookingsWorkbookAsync(
        BookingFilter filter, CancellationToken ct = default)
    {
        var rows = await bookings.Query(filter).ToListAsync(ct);

        var detail = new Sheet("Bookings")
            .WithHeaders(
                "Reference", "Status", "Date", "Slot", "Customer", "Phone", "Email",
                "Brand", "Model", "Body type", "Service", "Locality", "Price (₹)", "Booked on", "Notes")
            .WithWidths(18, 12, 13, 18, 22, 14, 26, 16, 18, 12, 24, 16, 12, 13, 40);

        foreach (var b in rows)
        {
            detail.AddRow(
                Cell.Text(b.Reference),
                Cell.Text(b.Status.ToString()),
                Cell.Date(b.PreferredDate),
                Cell.Text(b.PreferredSlot),
                Cell.Text(b.CustomerName),
                Cell.Text(b.Phone),
                Cell.Text(b.Email),
                Cell.Text(b.CarBrand),
                Cell.Text(b.CarModel),
                Cell.Text(b.BodyType),
                Cell.Text(b.Service?.Name),
                Cell.Text(b.Locality),
                Cell.Money(b.QuotedPrice),
                Cell.Date(DateOnly.FromDateTime(b.CreatedAt.LocalDateTime)),
                Cell.Text(b.Notes));
        }

        // Summary sheets computed from the same rows, so the workbook is self-consistent.
        var byStatus = new Sheet("By status")
            .WithHeaders("Status", "Bookings", "Value (₹)")
            .WithWidths(18, 12, 14);
        foreach (var g in rows.GroupBy(r => r.Status).OrderBy(g => g.Key))
        {
            byStatus.AddRow(
                Cell.Text(g.Key.ToString()),
                Cell.Number(g.Count()),
                Cell.Money(g.Sum(r => (long)r.QuotedPrice)));
        }

        var byService = new Sheet("By service")
            .WithHeaders("Service", "Bookings", "Value (₹)")
            .WithWidths(28, 12, 14);
        foreach (var g in rows.GroupBy(r => r.Service?.Name ?? "—")
                              .OrderByDescending(g => g.Count()))
        {
            byService.AddRow(
                Cell.Text(g.Key),
                Cell.Number(g.Count()),
                Cell.Money(g.Sum(r => (long)r.QuotedPrice)));
        }

        var byLocality = new Sheet("By locality")
            .WithHeaders("Locality", "Bookings", "Value (₹)")
            .WithWidths(24, 12, 14);
        foreach (var g in rows.GroupBy(r => r.Locality).OrderByDescending(g => g.Count()))
        {
            byLocality.AddRow(
                Cell.Text(g.Key),
                Cell.Number(g.Count()),
                Cell.Money(g.Sum(r => (long)r.QuotedPrice)));
        }

        var content = Build(detail, byStatus, byService, byLocality);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var scope = filter.IsFiltered ? "filtered" : "all";
        return (content, $"pitstop-bookings-{scope}-{stamp}.xlsx");
    }

    /// <summary>The dashboard's figures, as a workbook.</summary>
    public async Task<(byte[] Content, string FileName)> AnalyticsWorkbookAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var d = await analytics.BuildAsync(from, to, ct);

        var summary = new Sheet("Summary")
            .WithHeaders("Metric", "Value")
            .WithWidths(30, 18);
        summary.AddRow(Cell.Text("Period from"), Cell.Date(d.From));
        summary.AddRow(Cell.Text("Period to"), Cell.Date(d.To));
        summary.AddRow(Cell.Text("Jobs today"), Cell.Number(d.JobsToday));
        summary.AddRow(Cell.Text("Jobs next 7 days"), Cell.Number(d.JobsThisWeek));
        summary.AddRow(Cell.Text("Awaiting confirmation"), Cell.Number(d.PendingCount));
        summary.AddRow(Cell.Text("Bookings (all time)"), Cell.Number(d.TotalBookings));
        summary.AddRow(Cell.Text("Revenue booked"), Cell.Money(d.RevenueBooked));
        summary.AddRow(Cell.Text("Revenue completed"), Cell.Money(d.RevenueCompleted));
        summary.AddRow(Cell.Text("Average ticket"), Cell.Money(d.AverageTicket));
        summary.AddRow(Cell.Text("Cancellation rate (%)"), Cell.Text(d.CancellationRate.ToString("0.0")));

        var trend = new Sheet("Daily trend")
            .WithHeaders("Date", "Bookings", "Value (₹)")
            .WithWidths(14, 12, 14);
        foreach (var p in d.Trend)
        {
            trend.AddRow(Cell.Date(p.Date), Cell.Number(p.Count), Cell.Money(p.Revenue));
        }

        var services = NamedSheet("Top services", "Service", d.TopServices);
        var localities = NamedSheet("Top localities", "Locality", d.TopLocalities);
        var bodyTypes = NamedSheet("By body type", "Body type", d.ByBodyType);
        var slots = NamedSheet("Slot load", "Time slot", d.BySlot);

        var content = Build(summary, trend, services, localities, bodyTypes, slots);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        return (content, $"pitstop-analytics-{stamp}.xlsx");
    }

    private static Sheet NamedSheet(string title, string label, List<NamedCount> rows)
    {
        var sheet = new Sheet(title)
            .WithHeaders(label, "Bookings", "Value (₹)")
            .WithWidths(28, 12, 14);

        foreach (var r in rows)
        {
            sheet.AddRow(Cell.Text(r.Name), Cell.Number(r.Count), Cell.Money(r.Revenue));
        }

        return sheet;
    }
}
