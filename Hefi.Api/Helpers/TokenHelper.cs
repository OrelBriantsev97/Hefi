namespace Hefi.Api.Helpers;

/// <summary>
/// token helper :  generating a simple refresh token string.
/// </summary>
public static class TokenHelper
{
    public static string MintRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
