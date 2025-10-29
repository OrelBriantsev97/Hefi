using Hefi.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hefi.Mobile.Pages;


/// <summary>
/// Sign-up page -  Collects user credentials, calls IAuthService.RegisterAsync
/// and on success replaces the app root with MainPage
/// </summary>
public partial class SignUpPage : ContentPage
{
    private readonly IAuthService _auth;

    // initialize new SignUpPage with AuthService
    public SignUpPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
    }

    // navigate to LoginPage
    private async void OnGoLogin(object sender, EventArgs e)
    {
        var sp = Application.Current!.Handler!.MauiContext!.Services;
        var login = ActivatorUtilities.CreateInstance<LoginPage>(sp);
        await Navigation.PushAsync(login);
    }

    // validates input->register user -> persists tokens -> navigates to MainPage
    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        ResultLabel.Text = "";

        var name = NameEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ResultLabel.Text = "Please fill all fields.";
            return;
        }

        try
        {
            // registers and stores access/refresh tokens; throws on non-2xx
            var tokenPair = await _auth.RegisterAsync(name, email, password);

            //  replace root with MainPage
            var provider = Application.Current!.Handler!.MauiContext!.Services;
            var main = ActivatorUtilities.CreateInstance<MainPage>(provider);
            Application.Current!.Windows[0].Page = new NavigationPage(main);
        }
        catch (HttpRequestException ex)
        {
            // thrown by EnsureSuccessStatusCode when server returns non-2xx
            await DisplayAlert("Sign up failed", $"Server error: {ex.Message}", "OK");
        }
        catch (Exception ex)
        {
            ResultLabel.Text = ex.Message;
        }
    }
}
