using System.ComponentModel.DataAnnotations;
using CarWashWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarWashWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
public class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturn(returnUrl));
        }

        ViewData["Title"] = "Sign in — PitStop Admin";
        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        ViewData["Title"] = "Sign in — PitStop Admin";

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await signInManager.PasswordSignInAsync(
            input.Email, input.Password, input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user is not null)
            {
                user.LastLoginAt = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);
            }

            logger.LogInformation("Admin sign-in succeeded for {Email}.", input.Email);
            return LocalRedirect(SafeReturn(input.ReturnUrl));
        }

        // Deliberately vague: do not reveal whether the email exists.
        logger.LogWarning(
            "Failed admin sign-in for {Email} from {RemoteIp}.",
            input.Email, HttpContext.Connection.RemoteIpAddress);
        ModelState.AddModelError(string.Empty, "That email and password do not match.");
        return View(input);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Create an admin account straight from the login screen.
    ///
    /// Reachable without being signed in, so it must authorise itself: the form asks for
    /// an existing admin's credentials and verifies them before creating anything. The
    /// single exception is first run — when no admin exists there is nobody to authorise
    /// with, so the first account is open. That window closes the moment it is used.
    /// </summary>
    [HttpGet("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        ViewData["Title"] = "Add an admin — PitStop Admin";

        return View(new RegisterAdminInput
        {
            IsFirstRun = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 0,
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterAdminInput input)
    {
        ViewData["Title"] = "Add an admin — PitStop Admin";

        // Recomputed server-side — never trusted from the form, or anyone could post
        // IsFirstRun=true and skip authorisation entirely.
        var firstRun = (await userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 0;
        input.IsFirstRun = firstRun;

        if (!firstRun)
        {
            if (string.IsNullOrWhiteSpace(input.AuthorisingEmail) ||
                string.IsNullOrWhiteSpace(input.AuthorisingPassword))
            {
                ModelState.AddModelError(string.Empty,
                    "Enter the email and password of an existing admin to authorise this.");
                return View(input);
            }

            var approver = await userManager.FindByEmailAsync(input.AuthorisingEmail);
            var approved = approver is not null
                && await userManager.IsInRoleAsync(approver, Roles.Admin)
                && await userManager.CheckPasswordAsync(approver, input.AuthorisingPassword);

            if (!approved)
            {
                logger.LogWarning(
                    "Rejected admin self-registration for {Email} from {RemoteIp} — bad authorising credentials.",
                    input.Email, HttpContext.Connection.RemoteIpAddress);

                ModelState.AddModelError(string.Empty,
                    "Those authorising credentials are not an admin account.");
                return View(input);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = new AppUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FullName = input.FullName,
        };

        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(input);
        }

        await userManager.AddToRoleAsync(user, Roles.Admin);

        logger.LogInformation(
            "Admin account {Email} created via the login page ({Mode}).",
            input.Email, firstRun ? "first run" : $"authorised by {input.AuthorisingEmail}");

        TempData["Flash"] = $"Admin {input.FullName} created. Sign in below.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    public IActionResult Denied()
    {
        ViewData["Title"] = "Not allowed — PitStop Admin";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>Never redirect off-site after login, even if returnUrl is tampered with.</summary>
    private string SafeReturn(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin";
}

public class RegisterAdminInput
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

    /// <summary>Set by the controller, never trusted from the form.</summary>
    public bool IsFirstRun { get; set; }

    [EmailAddress]
    [Display(Name = "Authorising admin email")]
    public string? AuthorisingEmail { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Authorising admin password")]
    public string? AuthorisingPassword { get; set; }
}

public class LoginInput
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
