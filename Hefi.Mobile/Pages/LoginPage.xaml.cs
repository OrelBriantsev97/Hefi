using Hefi.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hefi.Mobile.Pages;

// Login page - validates credentials, calls IAuthService.LoginAsync
// on success replaces the app root with MainPage
public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;

    // initialize new LoginPage with AuthService
    public LoginPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    //  Handles the Login button click : validate input -> call backend -> navigate to MainPage
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ResultLabel.Text = "";
        ResultLabel.TextColor = Colors.Black;

        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ResultLabel.TextColor = Colors.Red;
            ResultLabel.Text = "Please enter email and password.";
            return;
        }

        try
        {
            // AuthService returns Token (access+refresh) and persists it; throws on non-2xx.
            var pair = await _authService.LoginAsync(email, password);

            // Success: tokens saved to SecureStorage by AuthService
            ResultLabel.TextColor = Colors.Green;
            ResultLabel.Text = "Login successful!";

            // Replace root with MainPage
            var sp = Application.Current!.Handler!.MauiContext!.Services;
            var main = ActivatorUtilities.CreateInstance<MainPage>(sp);
            Application.Current!.Windows[0].Page = new NavigationPage(main);
        }
        catch (HttpRequestException ex)
        {
            // Thrown by EnsureSuccessStatusCode when server returns non-2xx
            ResultLabel.TextColor = Colors.Red;
            ResultLabel.Text = $"Server error: {ex.Message}";
        }
        catch (Exception ex)
        {
            ResultLabel.TextColor = Colors.Red;
            ResultLabel.Text = $"Error: {ex.Message}";
        }
    }
}
