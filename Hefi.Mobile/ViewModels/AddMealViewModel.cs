using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using Hefi.Mobile.Models;
using Hefi.Mobile.Services;
using Hefi.Mobile.Pages;

namespace Hefi.Mobile.ViewModels;

/// <summary>
/// ViewModel for the "Add Meal" flow.
/// Handles food search, selection, and creates a meal via MealsService.
/// </summary>

public class AddMealViewModel : BindableObject
{
    private readonly HttpClient _http;
    private readonly MealsService _meals;       

    public string SearchQuery { get; set; } = ""; // user input for food search
    public ObservableCollection<FoodItem> Foods { get; set; } = new(); // search results
    public FoodItem? SelectedFood { get; set; }
    public string Grams { get; set; } = ""; //TODO : add selections for amount

    public ICommand SearchCommand { get; }
    public ICommand AddMealCommand { get; }
    public ICommand SelectFoodCommand { get; }

    //initalize ViewModel.
    /// <param name="http">HttpClient for public food search endpoints.</param>
    /// <param name="meals">Meals service for adding meals to the backend.</param>
    public AddMealViewModel(HttpClient http, MealsService meals) 
    {
        _http = http;
        _meals = meals;

        SearchCommand = new Command(async () => await SearchFoods());
        AddMealCommand = new Command(async () => await AddMeal());
        SelectFoodCommand = new Command<FoodItem>(async (food) => await OnFoodSelected(food));
    }

    //Calls  GET /foods/search? query =
    private async Task SearchFoods()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<FoodSearchResult>($"foods/search?query={SearchQuery}");
            Foods.Clear();
            if (result?.Foods != null)
            {
                foreach (var f in result.Foods)
                    Foods.Add(f);
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    //TODO: Implement AddMeal   
    private async Task AddMeal()
    {
        
    }

    /// Handles a food selection: opens amount popup, then posts the meal using MealsService
    private async Task OnFoodSelected(FoodItem? food)
    {
        if (food == null) return;

        var popup = new FoodAmountPopup(food);
        await App.Current.MainPage.Navigation.PushModalAsync(popup);

        popup.Disappearing += async (s, args) =>
        {
            if (popup.Result != null)
            {
                var id = await _meals.AddMealAsync(popup.Result);  
                await App.Current.MainPage.DisplayAlert(
                    "Saved",
                    $"Meal #{id} added: {popup.Result.FoodLabel} ({popup.Result.Amount}{popup.Result.Unit})",
                    "OK"
                );
            }
        };
    }
}
