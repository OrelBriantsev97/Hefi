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

    public string SearchQuery { get => _searchQuery; set { _searchQuery = value; OnPropertyChanged(); } }
    string _searchQuery = string.Empty; // user input for food search

    public string Amount { get => _amount; set { _amount = value; OnPropertyChanged(); } }
    string _amount = "100"; // default amount

    public List<string> Units { get; } = new() { "g", "ml", "piece" };
    public string SelectedUnit { get => _selectedUnit; set { _selectedUnit = value; OnPropertyChanged(); } }
    string _selectedUnit = "g"; // default unit gram

    public List<string> MealTypes { get; } = new() { "Breakfast", "Lunch", "Dinner", "Snack" };
    public string SelectedMealType { get => _mealType; set { _mealType = value; OnPropertyChanged(); } }
    string _mealType = "Lunch";
    public ObservableCollection<DisplayFood> Foods { get; set; } = new(); // search results


    public ICommand SearchCommand { get; }
    public ICommand AddMealCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ScanBarcodeCommand { get; }
    public ICommand PickPhotoCommand { get; }

    //initalize ViewModel.
    /// <param name="http">HttpClient for public food search endpoints.</param>
    /// <param name="meals">Meals service for adding meals to the backend.</param>
    public AddMealViewModel(HttpClient http, MealsService meals) 
    {
        _http = http;
        _meals = meals;

        SearchCommand = new Command(async () => await SearchFoods());
        AddMealCommand = new Command<DisplayFood>(async f => await AddMeal(f));
        ClearCommand = new Command(ClearForm);
        ScanBarcodeCommand = new Command(async () => await ScanBarcode());
        PickPhotoCommand = new Command(async () => await PickPhoto());
    }

    //Calls  GET /foods/search? query =
    private async Task SearchFoods(string? barcode = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery) && barcode is null)
            {
                await App.Current.MainPage.DisplayAlert("Search", "Type a food or scan a barcode.", "OK");
                return;
            }

            var url = barcode is null
                ? $"foods/search?query={Uri.EscapeDataString(SearchQuery)}"
                : $"foods/search?barcode={Uri.EscapeDataString(barcode)}";

            var result = await _http.GetFromJsonAsync<FoodSearchResult>(url);
            Foods.Clear();

            if (result?.Foods is null) return;

            double amount = double.TryParse(Amount, out var a) ? Math.Max(1, a) : 100;
            foreach (var f in result.Foods)
                Foods.Add(DisplayFood.From(f, amount, SelectedUnit));
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    //  
    private async Task AddMeal(DisplayFood? f)
    {
        var item = new MealItemCreate
        {
            FoodLabel = f.Label,
            Amount    = f.Amount,
            Unit      = f.Unit,
            Kcal      = (int)Math.Round(f.ScaledKcal),
            Protein   = f.ScaledProtein,
            Carbs     = f.ScaledCarbs,
            Fat       = f.ScaledFat,
            Sugar     = f.ScaledSugar
        };

        // Important: call the overload that accepts a MealItemCreate
        var id = await _meals.AddMealAsync(item);
    }

    // clear food search result
    void ClearForm()
    {
        SearchQuery = string.Empty;
        Amount = "100";
        SelectedUnit = "g";
        SelectedMealType = "Lunch";
        Foods.Clear();
        _lastPhotoPath = null;
    }

    // scan barcode and search food by it
    private async Task ScanBarcode()
    {
        var page = new Hefi.Mobile.Pages.BarcodeScanPage();
        await App.Current.MainPage.Navigation.PushAsync(page);
        var code = await page.WaitForResultAsync();
        if (!string.IsNullOrWhiteSpace(code))
            await SearchFoods(code); 
    }


    string? _lastPhotoPath;

    //TODO:add documentation
    async Task PickPhoto()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Meal photo" });
            if (result != null)
            {
                _lastPhotoPath = result.FullPath;
                await App.Current.MainPage.DisplayAlert("Photo", "Photo attached. It will be uploaded on Save.", "OK");
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Photo", ex.Message, "OK");
        }
    }
}

//representation of a food item with scaled macros for display
public class DisplayFood
{
    public int Id { get; set; }              // map from FdcId
    public string Label { get; set; } = "";  // map from Description
    public string? Brand { get; set; }       // map from BrandOwner
    public bool HasBrand => !string.IsNullOrWhiteSpace(Brand);

    public double ScaledKcal { get; set; }
    public double ScaledProtein { get; set; }
    public double ScaledCarbs { get; set; }
    public double ScaledFat { get; set; }
    public double ScaledSugar { get; set; }

    public double Amount { get; set; }
    public string Unit { get; set; } = "g";
    public string AmountUnitDisplay => $"{Amount:0.#} {Unit}";

    public static DisplayFood From(FoodItem f, double amount, string unit)
    {
        // USDA FDC common nutrient IDs
        const int ENERGY = 1008;   // kcal
        const int PROTEIN = 1003;
        const int CARBS = 1005;
        const int FAT = 1004;
        const int SUGARS = 2000;   // sometimes missing; we’ll fallback by name

        double Get(int id, string fallbackContains)
        {
            var byId = f.FoodNutrients.FirstOrDefault(n => n.NutrientId == id)?.Value;
            if (byId.HasValue) return byId.Value;

            var byName = f.FoodNutrients.FirstOrDefault(n =>
                n.NutrientName.Contains(fallbackContains, StringComparison.OrdinalIgnoreCase))?.Value;
            return byName ?? 0;
        }

        // FDC values are usually per 100 g; if not, this still scales reasonably.
        var per = 100.0;
        var factor = amount / per;

        var kcal = Get(ENERGY, "Energy");
        var prot = Get(PROTEIN, "Protein");
        var carb = Get(CARBS, "Carbohydrate");
        var fat = Get(FAT, "Fat");
        var sugar = Get(SUGARS, "Sugar");

        return new DisplayFood
        {
            Id = f.FdcId,
            Label = f.Description,
            Brand = f.BrandOwner,
            Amount = amount,
            Unit = unit,
            ScaledKcal = Math.Round(kcal * factor),
            ScaledProtein = Math.Round(prot * factor, 1),
            ScaledCarbs = Math.Round(carb * factor, 1),
            ScaledFat = Math.Round(fat * factor, 1),
            ScaledSugar = Math.Round(sugar * factor, 1)
        };
    }
}