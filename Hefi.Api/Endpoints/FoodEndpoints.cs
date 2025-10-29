using Hefi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hefi.Api.Endpoints;

/// <summary>
/// Endpoint  for food search features (USDA FoodData Central integration).
/// </summary>
public static class FoodEndpoints
{
    public static void MapFoodEndpoints(this WebApplication app)
    {
        // GET /foods/search?query=... : searches foods from USDA FoodData Central
        app.MapGet("/foods/search", async (
            [FromServices] FoodSearchService foodService,
            [FromQuery] string query
            ) =>
        {
            var result = await foodService.SearchFoodsAsync(query);
            return result is null ? Results.BadRequest(new { error = "Search failed" }) : Results.Ok(result);
        });
    }
}
