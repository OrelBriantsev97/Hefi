using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Hefi.Mobile.Services;
using Hefi.Mobile.Pages;
using Hefi.Mobile.ViewModels;


namespace Hefi.Mobile;

/// <summary>
/// builds app host , fonts,DI and services
/// </summary>
public static class MauiProgram
{
    /// 
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(_ => { });

        builder.Logging.AddDebug();

        //base address for API , using azure
        const string BaseUrl = "https://hefi-api-hzhrdhfxe6b2hddz.israelcentral-01.azurewebsites.net/";


        // Core authentication and token management
        builder.Services.AddSingleton<ITokenService, TokenService>();   
        builder.Services.AddTransient<AuthHttpHandler>();

        // registers AuthService interface with a typed HttpClient instance.
        // This client is preconfigured with the application's BaseUrl and a 15-second
        // timeout to prevent hanging requests.
        builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ApiClient general authenticated API utilities
        builder.Services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddHttpMessageHandler<AuthHttpHandler>();

        //register meals service
        builder.Services.AddHttpClient<MealsService>(client =>
        {
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddHttpMessageHandler<AuthHttpHandler>();

        // Register Pages and ViewModels
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SignUpPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();


        return builder.Build();
    }
}


public sealed class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http) => _http = http;

    public record HealthDto(string status, string name, DateTime time); 

    //calls GET /health endpoint
    public async Task<HealthDto> GetHealthAsync()
    {
        var json = await _http.GetStringAsync("health");
        var dto = System.Text.Json.JsonSerializer.Deserialize<HealthDto>(json, _jsonOpts);
        if (dto is null) throw new Exception("Empty /health response");
        return dto;
    }
    //calls GET /users/me endpoint
    public Task<HttpResponseMessage> GetMeAsync() => _http.GetAsync("users/me");

    //calls <c>GET /summary/{date:yyyy-MM-dd} endpoint
    public Task<HttpResponseMessage> GetSummaryAsync(DateTime date)
        => _http.GetAsync($"summary/{date:yyyy-MM-dd}");
}
