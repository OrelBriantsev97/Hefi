using System.Text;
using System.Text.Json;

namespace Hefi.Mobile.Services;

public record WorkoutDto(int Id, int UserId, string WorkoutType, int DurationMinutes, int CaloriesBurned, DateTime PerformedAt);
public record WorkoutCreate(string WorkoutType, int DurationMinutes, int CaloriesBurned, DateTime? PerformedAt);

/// <summary>
/// Service class responsible for interacting with the backend API's workout endpoints.
/// Handles fetching, creation, and daily calorie totals.
/// </summary>
public class WorkoutsService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // initialize new workout service via DI
    public WorkoutsService(HttpClient http) => _http = http;

    // Retrieves workouts from DB within optional date range
    public async Task<List<WorkoutDto>> GetAsync(DateTime? from = null, DateTime? to = null)
    {
        var url = "/workouts";
        var qs = new List<string>();
        if (from is not null) qs.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to is not null) qs.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (qs.Count > 0) url += "?" + string.Join("&", qs);
        var json = await _http.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<WorkoutDto>>(json, _json) ?? new();
    }
    // Sends a POST request to create a new workout entry.
    public async Task<int> AddAsync(WorkoutCreate w)
    {
        var content = new StringContent(JsonSerializer.Serialize(w, _json), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("/workouts", content);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    // Calculates the total calories burned from all workouts performed on a specific day.
    public async Task<int> GetTotalKcalAsync(DateTime? dayLocal = null)
    {
        var local = dayLocal?.Date ?? DateTime.Now.Date;
        var startLocal = local;
        var endLocal = local.AddDays(1);

        var from = Uri.EscapeDataString(startLocal.ToString("o"));
        var to = Uri.EscapeDataString(endLocal.ToString("o"));

        var json = await _http.GetStringAsync($"/workouts?from={from}&to={to}");
        var workouts = JsonSerializer.Deserialize<List<WorkoutDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<WorkoutDto>();

        return workouts.Sum(w => w.CaloriesBurned);
    }
}
