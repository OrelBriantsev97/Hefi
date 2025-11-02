using Hefi.Mobile.Services;
using Microsoft.Maui.Storage;

namespace Hefi.Mobile.Pages;

public sealed class DebugStoragePage : ContentPage
{
    private readonly ITokenService _tokens;
    private readonly Label _out;

    public DebugStoragePage(ITokenService tokens)
    {
        _tokens = tokens;

        _out = new Label
        {
            Margin = 16,
            LineBreakMode = LineBreakMode.WordWrap
        };

        Title = "Storage Debug";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "SecureStorage / Preferences Inspector", FontAttributes = FontAttributes.Bold, Margin = 16 },
                    new Button { Text = "Read tokens (TokenService.LoadAsync)", Command = new Command(async ()=> await DumpTokens()) },
                    new Button { Text = "Read SecureStorage key 'hefi_tokens_v1'", Command = new Command(async ()=> await ReadSecureRaw()) },
                    new Button { Text = "Read legacy key 'hefi.token'", Command = new Command(async ()=> await ReadLegacy()) },
                    new Button { Text = "Clear tokens", Command = new Command(async ()=> await ClearAll()) },
                    _out
                }
            }
        };
    }

    async Task DumpTokens()
    {
        var pair = await _tokens.LoadAsync();
        _out.Text = $"LoadAsync(): access len={(pair?.AccessToken?.Length ?? 0)}, refresh={(pair?.RefreshToken != null)}";
    }

    async Task ReadSecureRaw()
    {
        try
        {
            var raw = await SecureStorage.GetAsync("hefi_tokens_v1");
            _out.Text = $"SecureStorage['hefi_tokens_v1'] length={(raw?.Length ?? 0)}\n{raw}";
        }
        catch (Exception ex)
        {
            _out.Text = $"SecureStorage read failed: {ex}";
        }
    }

    async Task ReadLegacy()
    {
        var raw = await SecureStorage.GetAsync("hefi.token");
        _out.Text = $"SecureStorage['hefi.token'] length={(raw?.Length ?? 0)}\n{raw}";
    }

    async Task ClearAll()
    {
        try { SecureStorage.Remove("hefi_tokens_v1"); } catch { }
        try { SecureStorage.Remove("hefi.token"); } catch { }
        Preferences.Remove("hefi_tokens_v1_pref");
        _out.Text = "Cleared SecureStorage + Preferences.";
    }
}
