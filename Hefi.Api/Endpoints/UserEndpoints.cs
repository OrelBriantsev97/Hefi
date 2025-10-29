using System.Security.Claims;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;

namespace Hefi.Api.Endpoints;

/// <summary>
/// provides user-related endpoints (e.g., retrieving user profile information).
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // GET /users/profile : retrieves the profile of the authenticated user.    
        app.MapGet("/users/profile", [Authorize] async (ClaimsPrincipal user, IDbConnection db) =>
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim))
                return Results.Unauthorized();

            var userData = await db.QuerySingleOrDefaultAsync(
                @"SELECT id, name, email 
                  FROM users 
                  WHERE id = @id",
                new { id = int.Parse(idClaim) });

            if (userData is null)
                return Results.NotFound();

            return Results.Ok(userData);
        });
    }
}
