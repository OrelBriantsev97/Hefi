using System.Text.Json;
using Hefi.Mobile.Models;
using Microsoft.Maui.Storage;

namespace Hefi.Mobile.Services;

/// <summary>
/// Provides secure storage and retrieval of authentication tokens using SecureStorage/>.
/// This service is registered as a singleton and used by the authentication flow and HTTP handlers.
/// </summary>
public sealed class TokenService : ITokenService
{
   private const string Key = "hefi_tokens_v1";

    /// Saves the given token pair (access + refresh) securely in SecureStorage
    public async Task SaveAsync(Token pair)
   => await SecureStorage.SetAsync(Key, JsonSerializer.Serialize(pair));

    /// load the stored token pair from SecureStorage, or null if not found
    public async Task<Token?> LoadAsync()
   {
        var raw = await SecureStorage.GetAsync(Key);
        return string.IsNullOrEmpty(raw)
        ? null
         : JsonSerializer.Deserialize<Token>(raw);
   }

    // clears stored token by overwriting with empty string
    public Task ClearAsync()
       => SecureStorage.SetAsync(Key, "");
    }

//interface for token service
public interface ITokenService
{
    Task SaveAsync(Token pair);
    Task<Token?> LoadAsync();
    Task ClearAsync();
}

