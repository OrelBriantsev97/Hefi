using System.Net;
using Hefi.Mobile.Services;
using Hefi.Mobile.Models;

namespace Hefi.Mobile.Pages;

// loading page - Decides initial navigation based on token presence and a quick protected health check.
public partial class LoadingPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly ITokenService _tokens;
    private readonly ApiClient _api;

    // initialize new LoadingPage with AuthService, TokenService, and ApiClient
    public LoadingPage(IAuthService auth, ITokenService tokens, ApiClient api)
    {
        InitializeComponent();
        _auth   = auth   ?? throw new ArgumentNullException(nameof(auth));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _api    = api    ?? throw new ArgumentNullException(nameof(api));
    }

    // On first appearance, checks auth state and routes the user accordingly.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var pair = await _tokens.LoadAsync();
            if (pair is null)
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] No tokens stored -> SignUp");
                await Shell.Current.GoToAsync(nameof(SignUpPage));
                return;
            }

            var res = await _api.GetMeAsync();
            if (res.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] /users/me OK -> MainPage");
                await Shell.Current.GoToAsync(nameof(MainPage));
                return;
            }

            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] 401 -> trying manual refresh");
                var refreshed = await _auth.RefreshAsync();
                if (refreshed is not null)
                {
                    var retry = await _api.GetMeAsync();
                    if (retry.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadingPage] Retry OK -> MainPage");
                        await Shell.Current.GoToAsync(nameof(MainPage));
                        return;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Auth check failed ({(int)res.StatusCode}) -> SignUp");
            await Shell.Current.GoToAsync(nameof(SignUpPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Exception: {ex}");
            await Shell.Current.GoToAsync(nameof(SignUpPage));
        }
    }
}
