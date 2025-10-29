using System.Windows.Input;
using Hefi.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Hefi.Mobile.Pages;

namespace Hefi.Mobile.ViewModels;

/// <summary>
/// The main dashboard ViewModel for the mobile app.
/// Loads today's info(steps , meal kcal , workout kcal),
/// exposes a daily wellness tip, and provides navigation commands to add items.
/// </summary>
public sealed class MainViewModel : BindableObject
{
    private readonly IAuthService _auth;
    private readonly MealsService _meals;
    private readonly WorkoutsService _workouts;
    private readonly IServiceProvider _sp;

    /// <summary>
    /// initallize main view model
    /// </summary>
    /// <param name="auth">Authentication service (login/refresh/etc.).</param>
    /// <param name="meals">Service for meals and nutrition endpoints.</param>
    /// <param name="workouts">Service for workout endpoints.</param>
    /// <param name="sp">Root service provider used for page activation.</param>
    public MainViewModel(IAuthService auth, MealsService meals, WorkoutsService workouts, IServiceProvider sp)
    {
        _auth = auth;
        _meals = meals;
        _workouts = workouts;
        _sp = sp;

        // Navigates to AddMealPage 
        AddMealCommand = new Command(async () =>
        {
            var sp = Application.Current!.Handler!.MauiContext!.Services;
            var page = sp.GetRequiredService<AddMealPage>();
            await Application.Current!.MainPage!.Navigation.PushAsync(page);
        });


        // Navigates to AddWorkoutPage
        AddWorkoutCommand = new Command(async () =>
        {
            var page = ActivatorUtilities.CreateInstance<Hefi.Mobile.Pages.AddWorkoutPage>(_sp);
            await Shell.Current.Navigation.PushAsync(page);
        });

        DailyTip = PickDailyTip();
    }

    /// Loads dashboard data (greeting, subheader, daily totals).
    public async Task LoadAsync()
    {
        var name = await SecureStorage.GetAsync("hefi.name");
        Greeting = BuildGreeting(name);
        Subheader = "Are you ready to be healthy?";

        var today = DateTime.UtcNow.Date;

        try
        {
            MealKcalToday = await _meals.GetTotalKcalAsync(today);
        }
        catch { MealKcalToday = 0; }

        try
        {
            WorkoutKcalToday = await _workouts.GetTotalKcalAsync(today);
        }
        catch { WorkoutKcalToday = 0; }

        // Steps: placeholder now (TODO: integrate Google Fit / Health Connect / Apple HealthKit)
        StepsToday = 0;

        // Notify bindings that data has changed
        OnPropertyChanged(nameof(MealKcalToday));
        OnPropertyChanged(nameof(WorkoutKcalToday));
        OnPropertyChanged(nameof(StepsToday));
        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(Subheader));
        OnPropertyChanged(nameof(DailyTip));
    }

    // Builds a time-of-day greeting
    string BuildGreeting(string? name)
    {
        var hour = DateTime.Now.Hour;
        var part = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
        var who = string.IsNullOrWhiteSpace(name) ? "" : $" {name}";
        return $"{part}{who}";
    }

    string PickDailyTip()
    {
        var tips = new[]
        {
            "Drink a glass of water right now 💧",
            "Aim for 8k+ steps today 🚶",
            "Protein in every meal keeps you full 💪",
            "Go for color: add veggies to your next meal 🥗",
            "Short workout > no workout. 10 minutes counts! ⏱️"
        };
        var i = (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays % tips.Length;
        return tips[i];
    }

    // Bindables
    private string _greeting = "";
    public string Greeting { get => _greeting; set { _greeting = value; OnPropertyChanged(); } }

    private string _subheader = "";
    public string Subheader { get => _subheader; set { _subheader = value; OnPropertyChanged(); } }

    private int _mealKcalToday;
    public int MealKcalToday { get => _mealKcalToday; set { _mealKcalToday = value; OnPropertyChanged(); } }

    private int _workoutKcalToday;
    public int WorkoutKcalToday { get => _workoutKcalToday; set { _workoutKcalToday = value; OnPropertyChanged(); } }

    private int _steps;
    public int StepsToday { get => _steps; set { _steps = value; OnPropertyChanged(); } }

    private string _tip = "";
    public string DailyTip { get => _tip; set { _tip = value; OnPropertyChanged(); } }

    // opens add meal flow
    public ICommand AddMealCommand { get; }

    // opens add workout flow
    public ICommand AddWorkoutCommand { get; }
}
