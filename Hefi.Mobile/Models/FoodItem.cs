namespace Hefi.Mobile.Models;

// represents a single food item returned from the backend or the USDA FoodData Central API.

public class FoodItem
{
    public int FdcId { get; set; }
    public string Description { get; set; } = "";
    public string? BrandOwner { get; set; }

    public List<FoodNutrient> FoodNutrients { get; set; } = new();
}

// represents an individual nutrient entry associated with a food item.
public class FoodNutrient
{
    public int NutrientId { get; set; }
    public string NutrientName { get; set; } = "";
    public double Value { get; set; }
    public string UnitName { get; set; } = "";
}

// root object for a food search response containing a list of fooditems
public class FoodSearchResult
{
    public List<FoodItem> Foods { get; set; } = new();
}