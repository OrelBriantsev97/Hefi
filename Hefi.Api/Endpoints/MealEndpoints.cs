using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Hefi.Api.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Win32;

namespace Hefi.Api.Endpoints;

/// <summary>
/// meal endpoints: create meals, list meals (with optional date range),
///  fetch a specific meal with its items. Requires authorization.
/// </summary>
public static class MealEndpoints
{
    // registers all meal-related routes
    public static void MapMealEndpoints(this WebApplication app)
    {
        // POST /meals : creates a meal and its items in a single transaction. Returns 201 + id.
        // If totals are not provided, they are computed from the items.
        app.MapPost("/meals", [Authorize] async (HttpContext ctx, IDbConnection db, MealCreate req) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            if (req.Items is null || req.Items.Count == 0)
                return Results.BadRequest(new { error = "At least one item is required." });

            // Compute totals if not supplied by the client
            int totalKcal = req.TotalKcal ?? req.Items.Sum(i => i.Kcal);
            double totalProtein = req.TotalProtein ?? req.Items.Sum(i => i.Protein);
            double totalCarbs = req.TotalCarbs ?? req.Items.Sum(i => i.Carbs);
            double totalFat = req.TotalFat ?? req.Items.Sum(i => i.Fat);
            double totalSugar = req.TotalSugar ?? req.Items.Sum(i => i.Sugar);

            var eatenAt = req.EatenAt ?? DateTime.UtcNow;

            if (db.State != ConnectionState.Open)
                db.Open();

            using var tx = db.BeginTransaction();

            var mealId = await db.ExecuteScalarAsync<int>(@"
INSERT INTO meals (user_id, eaten_at, total_kcal, total_protein, total_carbs, total_fat, total_sugar)
VALUES (@UserId, @EatenAt, @TotalKcal, @TotalProtein, @TotalCarbs, @TotalFat, @TotalSugar)
RETURNING id;",
                new
                {
                    UserId = userId,
                    EatenAt = eatenAt,
                    TotalKcal = totalKcal,
                    TotalProtein = totalProtein,
                    TotalCarbs = totalCarbs,
                    TotalFat = totalFat,
                    TotalSugar = totalSugar
                }, tx);

            foreach (var it in req.Items)
            {
                await db.ExecuteAsync(@"
INSERT INTO meal_items (meal_id, food_label, grams, kcal, protein, carbs, fat, sugar)
VALUES (@MealId, @FoodLabel, @Grams, @Kcal, @Protein, @Carbs, @Fat, @Sugar);",
                    new
                    {
                        MealId = mealId,
                        it.FoodLabel,
                        it.Grams,
                        it.Kcal,
                        it.Protein,
                        it.Carbs,
                        it.Fat,
                        it.Sugar
                    }, tx);
            }

            tx.Commit();
            return Results.Created($"/meals/{mealId}", new { id = mealId });
        });

        // GET /meals : lists meals in reverse chronological order for the current user.
        app.MapGet("/meals", [Authorize] async (HttpContext ctx, IDbConnection db, DateTime? from, DateTime? to) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            if (db.State != ConnectionState.Open)
                db.Open();

            var headers = (await db.QueryAsync<MealHeader>(@"
SELECT
  id                               AS Id,
  user_id                          AS UserId,
  eaten_at                         AS EatenAt,
  COALESCE(total_kcal, 0)          AS TotalKcal,
  COALESCE(total_protein, 0)       AS TotalProtein,
  COALESCE(total_carbs, 0)         AS TotalCarbs,
  COALESCE(total_fat, 0)           AS TotalFat,
  COALESCE(total_sugar, 0)         AS TotalSugar
FROM meals
WHERE user_id = @UserId
  AND eaten_at >= COALESCE(@From::timestamptz, '-infinity'::timestamptz)
  AND eaten_at <  COALESCE(@To::timestamptz,  'infinity'::timestamptz)
ORDER BY eaten_at DESC;",
                new { UserId = userId, From = from, To = to })).ToList();

            var result = new List<MealDto>(headers.Count);
            foreach (var h in headers)
            {
                var items = (await db.QueryAsync<MealItemDto>(@"
SELECT id, food_label AS FoodLabel, grams AS Grams, kcal AS Kcal,
       protein AS Protein, carbs AS Carbs, fat AS Fat, sugar AS Sugar
FROM meal_items WHERE meal_id = @MealId;",
                    new { MealId = h.Id })).ToList();

                result.Add(new MealDto(
                    h.Id, h.UserId, h.EatenAt,
                    h.TotalKcal, h.TotalProtein, h.TotalCarbs, h.TotalFat, h.TotalSugar,
                    items));
            }

            return Results.Ok(result);
        });

        // GET /meals/{id: returns a single meal with its items for the current user.
        app.MapGet("/meals/{id:int}", [Authorize] async (HttpContext ctx, IDbConnection db, int id) =>
        {
            var userId = GetUserIdFromClaims(ctx.User);

            if (db.State != ConnectionState.Open)
                db.Open();

            var m = await db.QuerySingleOrDefaultAsync<MealHeader>(@"
SELECT
  id                               AS Id,
  user_id                          AS UserId,
  eaten_at                         AS EatenAt,
  COALESCE(total_kcal, 0)          AS TotalKcal,
  COALESCE(total_protein, 0)       AS TotalProtein,
  COALESCE(total_carbs, 0)         AS TotalCarbs,
  COALESCE(total_fat, 0)           AS TotalFat,
  COALESCE(total_sugar, 0)         AS TotalSugar
FROM meals WHERE id = @id AND user_id = @UserId;",
                new { id, UserId = userId });

            if (m is null) return Results.NotFound();

            var items = (await db.QueryAsync<MealItemDto>(@"
SELECT id, food_label AS FoodLabel, grams AS Grams, kcal AS Kcal,
       protein AS Protein, carbs AS Carbs, fat AS Fat, sugar AS Sugar
FROM meal_items WHERE meal_id = @MealId;", new { MealId = m.Id })).ToList();

            var dto = new MealDto(
                m.Id, m.UserId, m.EatenAt,
                m.TotalKcal, m.TotalProtein, m.TotalCarbs, m.TotalFat, m.TotalSugar,
                items);

            return Results.Ok(dto);
        });
    }

    // extract user ID from claims(NameIdentifier or 'sub').
    private static int GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.Parse(idStr!);
    }
}
