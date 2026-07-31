namespace CarWashWebsite.Infrastructure;

public class AdminPortOptions
{
    /// <summary>Ports the admin area is reachable on. Everything else 404s for /admin.</summary>
    public int[] Ports { get; set; } = [5300, 7300];

    /// <summary>
    /// Whether to split admin and public traffic by port at all. Turn this off on hosts that
    /// only expose a single port (Render, App Service, most containers behind one proxy) —
    /// there is no second listener to isolate onto, so leaving it on makes the admin area
    /// unreachable. Authentication still guards every admin action either way.
    /// </summary>
    public bool PortIsolation { get; set; } = true;
}

/// <summary>
/// Keeps the admin area and the public site on separate listeners.
///
///   · /admin/* on a public port  → 404 (indistinguishable from "no such page")
///   · public pages on the admin port → redirected to /admin
///
/// This is defence in depth, not the security boundary — authentication is. It means
/// that even a mistake in an [Authorize] attribute is not reachable from the public
/// address, and the admin port can be bound to a private interface or firewalled.
/// </summary>
public class AdminPortMiddleware(RequestDelegate next, AdminPortOptions options, ILogger<AdminPortMiddleware> logger)
{
    private static readonly string[] SharedPrefixes =
        ["/css", "/js", "/img", "/lib", "/favicon.svg", "/error"];

    public async Task InvokeAsync(HttpContext context)
    {
        var port = context.Connection.LocalPort;
        var isAdminPort = options.Ports.Contains(port);
        var path = context.Request.Path;
        var isAdminPath = path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase);

        if (isAdminPath && !isAdminPort)
        {
            logger.LogWarning(
                "Blocked admin request to {Path} on public port {Port} from {RemoteIp}",
                path, port, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (isAdminPort && !isAdminPath && !IsShared(path))
        {
            context.Response.Redirect("/admin");
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Assets the admin pages themselves need. Note "/" is deliberately NOT shared —
    /// hitting the root on the admin port should land on /admin, not the public site.
    /// </summary>
    private static bool IsShared(PathString path) =>
        path.Value is not null
        && SharedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
}

public static class AdminPortMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminPortIsolation(this IApplicationBuilder app, AdminPortOptions options) =>
        app.UseMiddleware<AdminPortMiddleware>(options);
}
