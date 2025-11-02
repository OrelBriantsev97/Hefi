using System.Net;
using Hefi.Mobile.Services;
using Hefi.Mobile.Models;

namespace Hefi.Mobile.Pages;

public partial class LoadingPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly ITokenService _tokens;
    private readonly ApiClient _api;

    public LoadingPage(IAuthService auth, ITokenService tokens, ApiClient api)
    {
        InitializeComponent();
        _auth   = auth   ?? throw new ArgumentNullException(nameof(auth));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _api    = api    ?? throw new ArgumentNullException(nameof(api));

        //TODO:dell later
        var tap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        int count = 0;
        tap.Tapped += async (_, __) =>
        {
            count++;
            if (count >= 3)
            {
                count = 0;
                var sp = Application.Current!.Handler!.MauiContext!.Services;
                await Navigation.PushAsync(sp.GetRequiredService<DebugStoragePage>());
            }
        };

        // Attach to the root view (Content), not the Page itself
        if (Content is View root)
        {
            root.GestureRecognizers.Add(tap);
        }
        else
        {
            // If Content isn't set yet, create a transparent Grid just to hold the gesture
            var grid = new Grid();
            grid.GestureRecognizers.Add(tap);
            Content = grid;
        }

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var sp = Application.Current!.Handler!.MauiContext!.Services;

        try
        {
            var pair = await _tokens.LoadAsync();
            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Loaded: access? {pair?.AccessToken != null}, refresh? {pair?.RefreshToken != null}");

            // No tokens => go to SignUp and STOP
            if (pair is null || string.IsNullOrWhiteSpace(pair.AccessToken))
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] No tokens stored -> SignUp");
                await Navigation.PushAsync(sp.GetRequiredService<SignUpPage>());
                return;
            }

            // Try a cheap authorized call
            var res = await _api.GetMeAsync();
            if (res.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] /users/me OK -> MainPage");
                await Navigation.PushAsync(sp.GetRequiredService<MainPage>());
                return;
            }

            // If token expired, try refresh once
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                System.Diagnostics.Debug.WriteLine("[LoadingPage] 401 -> trying manual refresh");
                var refreshed = await _auth.RefreshAsync();
                if (refreshed?.AccessToken is not null)
                {
                    var retry = await _api.GetMeAsync();
                    if (retry.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadingPage] Retry OK -> MainPage");
                        await Navigation.PushAsync(sp.GetRequiredService<MainPage>()); 
                        return;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Auth check failed -> SignUp (status {(int)res.StatusCode})");
            await Navigation.PushAsync(sp.GetRequiredService<SignUpPage>());
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Exception: {ex}");
            await Navigation.PushAsync(sp.GetRequiredService<SignUpPage>());
            return;
        }
    }
}
