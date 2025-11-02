using Microsoft.Extensions.DependencyInjection;
using Hefi.Mobile.ViewModels;

namespace Hefi.Mobile.Pages;

// Code-behind for the Add Meal page.
// Connects the XAML view to its AddMealViewModelvia dependency injection,
public partial class AddMealPage : ContentPage
{
    // Initializes the page and binds it to its view model
    public AddMealPage(AddMealViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        


    }

}
