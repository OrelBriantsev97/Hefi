using Microsoft.Maui.Controls;
using Hefi.Mobile.ViewModels;
using Hefi.Mobile.Helpers;
namespace Hefi.Mobile.Pages;

/// <summary>
/// home page , binds to MainViewModel and loads today's data
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    // initialize main page with its view model via DI
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    // Triggers initial data load each time the page becomes visible.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
   
    //TODO:  change to add command
    private async void OnAddWorkoutClicked(object sender, EventArgs e)
    {
        var addWorkoutlPage = ServiceHelper.GetService<AddWorkoutPage>();
        await Navigation.PushAsync(addWorkoutlPage);
    }
}
