using Microsoft.Extensions.DependencyInjection;
using Hefi.Mobile.ViewModels;

namespace Hefi.Mobile.Pages;

// Code-behind for the Add Meal page.
// Connects the XAML view to its AddMealViewModelvia dependency injection,
public partial class AddMealPage : ContentPage
{
    // Initializes the page and binds it to its view model
    public AddMealPage()
    {
        InitializeComponent(); 

        var sp = Application.Current?.Handler?.MauiContext?.Services
                 ?? throw new InvalidOperationException("ServiceProvider unavailable");

        BindingContext = sp.GetRequiredService<AddMealViewModel>();
    }

}
