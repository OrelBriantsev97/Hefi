using System.Net.Http.Headers;
using System.Threading;

namespace Hefi.Mobile.Services;

/// <summary>
/// HTTP handler that attaches users tokens to outgoing requests
/// </summary>
public sealed class AuthHttpHandler : DelegatingHandler
{
    private readonly IAuthService _auth;
    private readonly ITokenService _tokens;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    // initialize new AuthHttpHandler with AuthService and TokenService
    public AuthHttpHandler(IAuthService auth, ITokenService tokens)
    {
        _auth = auth; _tokens = tokens;
    }

    // send HTTP request, attaching access token and handling 401 by refreshing token once
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // attach current access token
        var pair = await _tokens.LoadAsync();
        if (pair?.AccessToken is { Length: > 0 })
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pair.AccessToken);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) //handle non-401
            return response;

        // dispose the 401 and try refresh once (single-flight)
        response.Dispose();

        await _refreshLock.WaitAsync(ct);
        try
        {
            // maybe another request refreshed already
            pair = await _tokens.LoadAsync();

            // Attempt refresh using stored refresh token.
            var refreshed = await _auth.RefreshAsync();
            if (refreshed is null) return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

            var clone = await CloneAsync(request);
            clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken); //Attach fresh access token and retry once.
            return await base.SendAsync(clone, ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // Clone HttpRequestMessage since it can only be sent once
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy headers except Authorization
        foreach (var h in req.Headers)
            if (h.Key != "Authorization")
                clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            var newContent = new StreamContent(ms);
            foreach (var h in req.Content.Headers)
                newContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
            clone.Content = newContent;
        }

        clone.Version = req.Version;
        return clone;
    }
}
