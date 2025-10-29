using System.Text.Json;
using Hefi.Api.Dtos;

namespace Hefi.Api.Services;

/// <summary>
/// Service that integrates with the USDA FoodData Central API to search for food nutrition data.
/// </summary>
public class FoodSearchService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    // initialize new srvice class
    public FoodSearchService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = "OXY0amfiO7XAp4XmEiIYnXfVrzssJfXHz2trwF5V"; // TODO: Move to Azure App Settings
    }

    /// <summary>
    /// Searches the USDA FoodData Central API for food items matching the given query.
    /// </summary>
    /// <param name="query">The user-entered search term
    public async Task<FoodSearchResult?> SearchFoodsAsync(string query)
    {
        var url = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={Uri.EscapeDataString(query)}&api_key={_apiKey}";

        Console.WriteLine($"[FoodSearchService] Requesting: {url}");
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;
        Console.WriteLine($"[FoodSearchService] Response status: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[FoodSearchService] Response body: {json}");
        return JsonSerializer.Deserialize<FoodSearchResult>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
