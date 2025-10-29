using Hefi.Mobile.Models;

namespace Hefi.Mobile.Pages;

public partial class FoodAmountPopup : ContentPage
{
    private readonly FoodItem _food;

    public MealItemCreate? Result { get; private set; }

    public FoodAmountPopup(FoodItem food)
    {
        InitializeComponent();
        _food = food;
        FoodLabel.Text = food.Description;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        Result = null;
        await Navigation.PopModalAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (!double.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
        {
            await DisplayAlert("Error", "Enter a valid amount", "OK");
            return;
        }

        if (UnitPicker.SelectedItem == null)
        {
            await DisplayAlert("Error", "Select a unit", "OK");
            return;
        }

        string unit = UnitPicker.SelectedItem.ToString() ?? "grams";
        double factor = unit == "grams" ? amount / 100.0 : 1; // scale only for grams

        var kcal = _food.FoodNutrients.FirstOrDefault(n => n.NutrientName.Contains("Energy"))?.Value ?? 0;
        var protein = _food.FoodNutrients.FirstOrDefault(n => n.NutrientName.Contains("Protein"))?.Value ?? 0;
        var fat = _food.FoodNutrients.FirstOrDefault(n => n.NutrientName.Contains("Fat"))?.Value ?? 0;
        var carbs = _food.FoodNutrients.FirstOrDefault(n => n.NutrientName.Contains("Carbohydrate"))?.Value ?? 0;
        var sugar = _food.FoodNutrients.FirstOrDefault(n => n.NutrientName.Contains("Sugars"))?.Value ?? 0;

        Result = new MealItemCreate
        {
            FoodLabel = _food.Description,
            Amount = amount,
            Unit = unit,
            Kcal = (int)(kcal * factor),
            Protein = protein * factor,
            Carbs = carbs * factor,
            Fat = fat * factor,
            Sugar = sugar * factor
        };

        await Navigation.PopModalAsync();
    }
}
