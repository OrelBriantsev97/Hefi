using System.Net.Http.Json;
using System.Text.Json;
using Hefi.Mobile.Models;

namespace Hefi.Mobile.Services;

/// <summary>
/// Handles all authentication operations including registration, login, token refresh, and logout.  
/// Uses ITokenService for secure token storage and a DI-provided
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ITokenService _tokens;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // initallize new AuthService with HttpClient and TokenService
    public AuthService(HttpClient http, ITokenService tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    // Registers a new user and saves the returned token pair to secure storage.
    public async Task<Token> RegisterAsync(string name, string email, string password)
    {
        var res = await _http.PostAsJsonAsync("auth/register", new { name, email, password });
        res.EnsureSuccessStatusCode();

        //extract and stores token pain

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(_json);
        var pair = new Token
        {
            AccessToken = json.GetProperty("accessToken").GetString()!,
            RefreshToken = json.GetProperty("refreshToken").GetString()!
        };
        await _tokens.SaveAsync(pair);
        return pair;
    }

    // Logs in an existing user
    public async Task<Token> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("auth/login", new { email, password });
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(_json);
        var pair = new Token
        {
            AccessToken = json.GetProperty("accessToken").GetString()!,
            RefreshToken = json.GetProperty("refreshToken").GetString()!
        };
        await _tokens.SaveAsync(pair);
        return pair;
    }

    // Refreshes the access token using the stored refresh token.
    public async Task<Token?> RefreshAsync()
    {
        var current = await _tokens.LoadAsync();
        if (current is null || string.IsNullOrWhiteSpace(current.RefreshToken)) return null;

        var res = await _http.PostAsJsonAsync("auth/refresh", new { refreshToken = current.RefreshToken });
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(_json);
        var pair = new Token
        {
            AccessToken = json.GetProperty("accessToken").GetString()!,
            RefreshToken = json.GetProperty("refreshToken").GetString()!
        };
        await _tokens.SaveAsync(pair);
        return pair;
    }

    public Task<Token?> GetTokensAsync() => _tokens.LoadAsync(); //load current tokens

    // Logs out the user and clears stored tokens.
    public async Task LogoutAsync()
    {
        try
        {
            var current = await _tokens.LoadAsync();
            if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
                await _http.PostAsJsonAsync("auth/logout", new { refreshToken = current.RefreshToken });
        }
        catch { /* ignore network errors on logout */ }
        await _tokens.ClearAsync();
    }
}

// interface for AuthService
public interface IAuthService
{
    Task<Token> RegisterAsync(string name, string email, string password);
    Task<Token> LoginAsync(string email, string password);
    Task<Token?> RefreshAsync();
    Task<Token?> GetTokensAsync();
    Task LogoutAsync();
}
