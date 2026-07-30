using CarWashWebsite.Models;
using CarWashWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class DashboardController(AnalyticsService analytics, ExportService exports) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (start, end) = Window(from, to);

        ViewData["Title"] = "Dashboard — PitStop Admin";
        ViewData["ActiveNav"] = "dashboard";
        return View(await analytics.BuildAsync(start, end, ct));
    }

    [HttpGet("analytics.xlsx")]
    public async Task<IActionResult> ExportAnalytics(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (start, end) = Window(from, to);
        var (content, fileName) = await exports.AnalyticsWorkbookAsync(start, end, ct);

        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    /// <summary>Defaults to the last 30 days through the next 30, and never runs backwards.</summary>
    private static (DateOnly From, DateOnly To) Window(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? today.AddDays(-30);
        var end = to ?? today.AddDays(30);
        return end < start ? (end, start) : (start, end);
    }
}
