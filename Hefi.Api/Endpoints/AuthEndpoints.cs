using System.Data;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Hefi.Api.Models;
using System.Security.Cryptography;
using Hefi.Api.Dtos;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

/// <summary>
/// Authentication endpoint group: register, login, validate, logout, and refresh.
/// issues JWT access tokens and manages refresh tokens persisted in the database.
/// </summary>

namespace Hefi.Api.Endpoints;

public static class AuthEndpoints
{
    // TODO: check if needed
    private static async Task EnsureOpenAsync(IDbConnection db)
    {
        if (db.State != ConnectionState.Open)
            await ((DbConnection)db).OpenAsync();
    }

    // registers all authentication routes.
    public static void MapAuthEndpoints(this WebApplication app, IConfiguration config)
    {
        var jwtSection = config.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var jwtIssuer = jwtSection["Issuer"] ?? "Hefi";
        var jwtAudience = jwtSection["Audience"] ?? "HefiUsers";
        var jwtExpiresHours = int.TryParse(jwtSection["ExpiresHours"], out var h) ? h : 2;

        // POST /auth/register -  creates a user, returns access/refresh tokens (201 Created on success
        app.MapPost("auth/register", async (HttpContext ctx, IDbConnection db, UserCreate req) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Name) ||
                    string.IsNullOrWhiteSpace(req.Email) ||
                    string.IsNullOrWhiteSpace(req.Password))
                {
                    return Results.BadRequest(new { error = "Name, Email, and Password are required." });
                }
                // Hash password and insert user
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
                int id;
                try
                {
                    id = await db.ExecuteScalarAsync<int>(
                        @"INSERT INTO users (name, email, password_hash)
                  VALUES (@Name, @Email, @PasswordHash)
                  RETURNING id;",
                        new { req.Name, req.Email, PasswordHash = passwordHash });
                }
                catch (PostgresException pgex) when (pgex.SqlState == "23505")
                {
                    return Results.Conflict(new { error = "Email already registered." });
                }

                // access token
                var token = GenerateJwtToken(id, req.Email, jwtKey, jwtIssuer, jwtAudience, jwtExpiresHours);

                var rt = NewRefreshToken();
                var rtHash = HashToken(rt);
                var rtExpires = DateTime.UtcNow.AddYears(1);

                try
                {
                    await db.ExecuteAsync(@"
                INSERT INTO refresh_tokens (user_id, token_hash, expires_at, user_agent, ip_address)
                VALUES (@UserId, @Hash, @Expires, @UA, @IP)",
                        new
                        {
                            UserId = id,
                            Hash = rtHash,
                            Expires = rtExpires,
                            UA = ctx.Request.Headers.UserAgent.ToString(),
                            IP = ctx.Connection.RemoteIpAddress?.ToString()
                        });
                }
                catch (Exception ex)
                {
                    // If refresh insert fails, don't 500 silently—tell us why.
                    return Results.Problem($"Failed to save refresh token: {ex.Message}", statusCode: 500);
                }

                return Results.Created($"/users/{id}", new
                {
                    id,
                    name = req.Name,
                    email = req.Email,
                    accessToken = token,
                    refreshToken = rt
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Register failed: {ex.Message}", statusCode: 500);
            }
        });



        // POST /auth/login : verifies credentials, returns access/refresh tokens (200 OK on success).
        app.MapPost("auth/login", async (HttpContext ctx, IDbConnection db, UserLoginRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Email and Password are required." });

            var user = await db.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT id, email, password_hash FROM users WHERE email = @Email LIMIT 1",
                new { req.Email });

            if (user is null) return Results.Unauthorized();

            bool ok = BCrypt.Net.BCrypt.Verify(req.Password, (string)user.password_hash);
            if (!ok) return Results.Unauthorized();

            // access token
            string accessToken = GenerateJwtToken((int)user.id, (string)user.email, jwtKey, jwtIssuer, jwtAudience, jwtExpiresHours);

            // refresh token (14 days)
            var rt = NewRefreshToken();
            var rtHash = HashToken(rt);
            var rtExpires = DateTime.UtcNow.AddYears(1);

            await db.ExecuteAsync(@"
                INSERT INTO refresh_tokens (user_id, token_hash, expires_at, user_agent, ip_address)
                VALUES (@UserId, @Hash, @Expires, @UA, @IP)",
                new
                {
                    UserId = (int)user.id,
                    Hash = rtHash,
                    Expires = rtExpires,
                    UA = ctx.Request.Headers.UserAgent.ToString(),
                    IP = ctx.Connection.RemoteIpAddress?.ToString()
                });

            return Results.Ok(new
            {
                id = (int)user.id,
                email = (string)user.email,
                accessToken,
                refreshToken = rt
            });
        });

        // GET /auth/validate : verify/inspect current identity.
        app.MapGet("auth/validate", (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;

            return Results.Ok(new
            {
                status = "valid",
                userId,
                email
            });
        })
        .RequireAuthorization();

        // POST /auth/logout :revokes a refresh token 
        app.MapPost("auth/logout", async (HttpContext ctx, IDbConnection db) =>
        {
            var input = await ctx.Request.ReadFromJsonAsync<RefreshRequest>();
            if (input is null || string.IsNullOrWhiteSpace(input.RefreshToken))
                return Results.BadRequest(new { error = "refreshToken required" });

            var hash = HashToken(input.RefreshToken);
            var affected = await db.ExecuteAsync(
            "UPDATE refresh_tokens SET revoked_at = now() WHERE token_hash = @Hash AND revoked_at IS NULL",
            new { Hash = hash });

            return Results.Ok(new { revoked = affected > 0 });
        });


        // POST /auth/refresh: exchanges a live refresh token for a new access + refresh token pair.
        // revokes the old refresh token
        app.MapPost("auth/refresh", async (IDbConnection db, RefreshRequest input, HttpContext ctx) =>
        {
            try
            {
                if (input is null || string.IsNullOrWhiteSpace(input.RefreshToken))
                    return Results.BadRequest(new { error = "refreshToken required" });

                // 1) Find a live (not revoked, not expired) refresh token
                var hash = HashToken(input.RefreshToken);

                var rt = await db.QuerySingleOrDefaultAsync<RefreshRow>(@"
            SELECT id, user_id AS UserId, token_hash AS TokenHash, expires_at AS ExpiresAt, revoked_at AS RevokedAt
            FROM refresh_tokens
            WHERE token_hash = @Hash
              AND revoked_at IS NULL
              AND expires_at > now()
            LIMIT 1",
                    new { Hash = hash });

                if (rt is null)
                    return Results.Unauthorized();

                // 2) Load the user
                var user = await db.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT id, email FROM users WHERE id = @id",
                    new { id = rt.UserId });

                if (user is null)
                    return Results.Unauthorized();

                // 3) Create new access + refresh
                var newAccess = GenerateJwtToken((int)user.id, (string)user.email, jwtKey, jwtIssuer, jwtAudience, jwtExpiresHours);
                var newRefresh = NewRefreshToken();
                var newHash = HashToken(newRefresh);
                var newExpires = DateTime.UtcNow.AddYears(1);

                // 4) Revoke old RT and insert new RT
                //    (two SQL statements in one command; Dapper/Npgsql will auto-open/close)
                var sql = @"
            UPDATE refresh_tokens
               SET revoked_at = now(),
                   replaced_by_token_hash = @NewHash
             WHERE id = @Id;

            INSERT INTO refresh_tokens (user_id, token_hash, expires_at, user_agent, ip_address)
            VALUES (@UserId, @Hash, @Expires, @UA, @IP);
        ";

                var affected = await db.ExecuteAsync(sql, new
                {
                    NewHash = newHash,
                    Id = rt.Id,
                    UserId = (int)user.id,
                    Hash = newHash,
                    Expires = newExpires,
                    UA = ctx.Request.Headers.UserAgent.ToString(),
                    IP = ctx.Connection.RemoteIpAddress?.ToString()
                });

                // If needed, you can check affected >= 1, but both statements are atomic individually.

                return Results.Ok(new { accessToken = newAccess, refreshToken = newRefresh });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Refresh failed: {ex.Message}", statusCode: 500);
            }
        });


    }

    // generates a signed JWT with name identifier + email
    private static string GenerateJwtToken(int userId, string email, string key, string issuer, string audience, int expiresHours)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    // returns a cryptographically secure, base64-encoded refresh token.
    private static string NewRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes); // return to client
    }

    // computes a hex-encoded SHA-256 hash of a refresh token (stored in DB).
    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token))); // store in DB
    }

    private record RefreshRequest(string RefreshToken);

    private sealed class RefreshRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

}
