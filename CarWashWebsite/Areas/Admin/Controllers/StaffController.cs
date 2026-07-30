using System.ComponentModel.DataAnnotations;
using CarWashWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/staff")]
[Authorize(Policy = Policies.AdminOnly)]
public class StaffController(
    UserManager<AppUser> userManager,
    ILogger<StaffController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Staff — PitStop Admin";
        ViewData["ActiveNav"] = "staff";

        var users = await userManager.Users.OrderBy(u => u.FullName).ToListAsync(ct);

        var rows = new List<StaffRow>(users.Count);
        foreach (var user in users)
        {
            rows.Add(new StaffRow
            {
                User = user,
                Roles = [.. await userManager.GetRolesAsync(user)],
                IsLockedOut = await userManager.IsLockedOutAsync(user),
            });
        }

        return View(new StaffPageViewModel { Staff = rows, Form = new StaffInput() });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffInput form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await IndexWithForm(form, ct);
        }

        var user = new AppUser
        {
            UserName = form.Email,
            Email = form.Email,
            EmailConfirmed = true,
            FullName = form.FullName,
        };

        var created = await userManager.CreateAsync(user, form.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return await IndexWithForm(form, ct);
        }

        var role = form.Role == Roles.Admin ? Roles.Admin : Roles.Staff;
        await userManager.AddToRoleAsync(user, role);

        logger.LogInformation(
            "{Actor} created {Role} account {Email}.", User.Identity?.Name, role, form.Email);

        TempData["Flash"] = $"{form.FullName} added as {role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // Never let the last admin delete themselves out of the system.
        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
            {
                TempData["Flash"] = "That is the only admin account — it cannot be removed.";
                return RedirectToAction(nameof(Index));
            }
        }

        await userManager.DeleteAsync(user);
        logger.LogInformation("{Actor} deleted account {Email}.", User.Identity?.Name, user.Email);

        TempData["Flash"] = $"{user.FullName} removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/unlock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        TempData["Flash"] = $"{user.FullName} unlocked.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> IndexWithForm(StaffInput form, CancellationToken ct)
    {
        ViewData["Title"] = "Staff — PitStop Admin";
        ViewData["ActiveNav"] = "staff";

        var users = await userManager.Users.OrderBy(u => u.FullName).ToListAsync(ct);
        var rows = new List<StaffRow>(users.Count);
        foreach (var user in users)
        {
            rows.Add(new StaffRow
            {
                User = user,
                Roles = [.. await userManager.GetRolesAsync(user)],
                IsLockedOut = await userManager.IsLockedOutAsync(user),
            });
        }

        return View(nameof(Index), new StaffPageViewModel { Staff = rows, Form = form });
    }
}

public class StaffPageViewModel
{
    public required List<StaffRow> Staff { get; init; }
    public required StaffInput Form { get; init; }
}

public class StaffRow
{
    public required AppUser User { get; init; }
    public required List<string> Roles { get; init; }
    public bool IsLockedOut { get; init; }
}

public class StaffInput
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter an email.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Set a password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "At least 10 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string Role { get; set; } = Roles.Staff;
}
