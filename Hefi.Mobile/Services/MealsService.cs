using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Hefi.Mobile.Models;
using System.Linq;


/// <summary>
/// Provides access to meal-related endpoints in the backend API.
/// Handles fetching user meals, adding new meals, and calculating daily calorie totals.
/// </summary>
public class MealsService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public MealsService(HttpClient http) { _http = http; } // initallize new meal service

    /// <summary>
    /// Retrieves all meals from the DB within an optional date range.
    /// </summary>
    /// <param name="from">Start date (inclusive, in UTC or ISO 8601 format).</param>
    /// <param name="to">End date (exclusive, in UTC or ISO 8601 format).</param>
    /// <returns>A list ofMealDto records representing meals in the range.</returns>
    public async Task<List<MealDto>> GetMealsAsync(DateTime? from = null, DateTime? to = null)
    {
        var url = "/meals";
        var qs = new List<string>();
        if (from is not null) qs.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to is not null) qs.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (qs.Count > 0) url += "?" + string.Join("&", qs);

        var json = await _http.GetStringAsync(url);
        var data = JsonSerializer.Deserialize<List<MealDto>>(json, _json) ?? new();
        return data;
    }

    // adds a new meal entry and return its assigned ID
    public async Task<int> AddMealAsync(MealItemCreate item)
    { 
        var payload = new { items = new[] { item } };
        var content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("/meals", content);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    // calculats total kcal for a given day and returns the sum
    public async Task<int> GetTotalKcalAsync(DateTime? dayLocal = null)
    {
      
        var local = dayLocal?.Date ?? DateTime.Now.Date;
        var startLocal = local;
        var endLocal = local.AddDays(1);

        var from = Uri.EscapeDataString(startLocal.ToString("o"));
        var to = Uri.EscapeDataString(endLocal.ToString("o"));

        var json = await _http.GetStringAsync($"/meals?from={from}&to={to}");
        var meals = JsonSerializer.Deserialize<List<MealDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<MealDto>();

        return meals.Sum(m => m.TotalKcal);
    }

}
