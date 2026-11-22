using System.Security.Claims;
using Analytical_Engine.Models;

namespace Analytical_Engine.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(rawId, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identifier in token.");
        }

        return userId;
    }

    public static bool IsDataAdministrator(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(Roles.DataAdministrator);
    }
}