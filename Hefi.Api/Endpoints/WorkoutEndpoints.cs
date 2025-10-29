using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Hefi.Api.Dtos;

namespace Hefi.Api.Endpoints;

/// <summary>
/// Registers workout-related routes 
/// </summary>
public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this WebApplication app)
    {
        // POST /workouts : creates a workout entry. Returns 201 + id.
        app.MapPost("/workouts", [Authorize] async (HttpContext ctx, IDbConnection db, WorkoutCreate req) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            if (string.IsNullOrWhiteSpace(req.WorkoutType) ||
                req.DurationMinutes < 0 ||
                req.CaloriesBurned < 0)
            {
                return Results.BadRequest(new { error = "Invalid workout data." });
            }

            var sql = @"
                INSERT INTO workouts (user_id, type, subtype, duration_min, intensity, calories_burned, performed_at, source, notes)
                VALUES (@UserId, @Type, @Subtype, @DurationMin, @Intensity, @CaloriesBurned, COALESCE(@PerformedAt, NOW()), @Source, @Notes)
                RETURNING id;";

            var id = await db.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                Type = req.WorkoutType,
                Subtype = (string?)null,
                DurationMin = req.DurationMinutes,
                Intensity = (string?)null,
                req.CaloriesBurned,
                req.PerformedAt,
                Source = (string?)"manual",
                Notes = (string?)null
            });

            return Results.Created($"/workouts/{id}", new { id });
        });

        // GET /workouts : lists workouts for the current user, newest first.
        app.MapGet("/workouts", [Authorize] async (HttpContext ctx, IDbConnection db, DateTime? from, DateTime? to) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            var sql = @"
                SELECT id,
                user_id      AS UserId,
                type         AS WorkoutType,
                duration_min AS DurationMinutes,
                calories_burned AS CaloriesBurned,
                performed_at AS PerformedAt
                FROM workouts WHERE user_id = @UserId
                AND performed_at >= COALESCE(@From::timestamptz, '-infinity'::timestamptz)
                AND performed_at <  COALESCE(@To::timestamptz,  'infinity'::timestamptz)
                ORDER BY performed_at DESC;";

            var rows = await db.QueryAsync<WorkoutDto>(sql, new { UserId = userId, From = from, To = to });
            return Results.Ok(rows);
        });

        // GET /workouts/{ id} :returns a single workout owned by the current user.
        app.MapGet("/workouts/{id:int}", [Authorize] async (HttpContext ctx, IDbConnection db, int id) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            var sql = @"
                SELECT id,
                user_id      AS UserId,
                type         AS WorkoutType,
                duration_min AS DurationMinutes,
                calories_burned AS CaloriesBurned,
                performed_at AS PerformedAt
                FROM workouts WHERE id = @id AND user_id = @UserId;";

            var row = await db.QuerySingleOrDefaultAsync<WorkoutDto>(sql, new { id, UserId = userId });
            return row is null ? Results.NotFound() : Results.Ok(row);
        });
    }

    // extracts user ID from JWT claims (NameIdentifier or 'sub').
    private static int GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.Parse(idStr!);
    }
}
