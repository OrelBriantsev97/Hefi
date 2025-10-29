using Hefi.Mobile.Services;

namespace Hefi.Mobile.Pages;

/// <summary>
/// workoutPage -  Displays the user's logged workouts and navigate to AddWorkoutPage to create a new workout entry.
/// </summary>
public partial class WorkoutsPage : ContentPage
{
    private readonly WorkoutsService _svc;
    private readonly AddWorkoutPage _add;

    // initialize new workouts page via DI
    public WorkoutsPage(WorkoutsService svc, AddWorkoutPage add)
    {
        InitializeComponent();
        _svc = svc;
        _add = add;
    }

    // loads workouts each time the page becomes visible.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        List.ItemsSource = await _svc.GetAsync();
    }

    // navigates to the Add Workout page when the user clicks the Add button
    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(_add);
    }
}
