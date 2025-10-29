using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Hefi.Api;

/// <summary>
/// provides helper methods for creating and managing JWT access tokens
/// and secure refresh tokens within the Hefi authentication system.
/// </summary>
public static class AuthHelpers
{
    /// <summary>
    /// Creates a signed JSON Web Token (JWT) containing the user ID and email claims.
    /// </summary>
    /// <param name="userId">users id</param> ,<param name="email"> user’s email address </param>
    /// <param name="issuer">token issuer </param> <param name="audience"> token audience</param>
    /// <param name="key">key to validate token</param> / <param name="expiresHours">The number of hours before the token expires.</param>
    /// <returns>A signed JWT string encoded with base64 that can be returned to the client.</returns>
    public static string CreateJwt(int userId, string email, string issuer, string audience, string key, int expiresHours)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, email)
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Generates a new secure refresh token.
    public static string NewRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes); // return raw token to client
    }

    // Creates a SHA-256 hash of a refresh token for secure database storage.
    public static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token))); // DB stores hash only
    }
}
