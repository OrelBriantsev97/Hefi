namespace Hefi.Api.Dtos;


// represents the root response from the USDA FoodData Central API search endpoint.
public class FoodSearchResult
{
    public List<FoodItem> Foods { get; set; } = new();
}

// represents a single food item result in a USDA search response.
public class FoodItem
{
    public int FdcId { get; set; }
    public string Description { get; set; } = "";
    public string? BrandOwner { get; set; }
    public List<FoodNutrient> FoodNutrients { get; set; } = new();
}

// represents a nutrient entry for a food item (e.g., Calories, Protein, Carbohydrates).
public class FoodNutrient
{
    public int NutrientId { get; set; }
    public string NutrientName { get; set; } = "";
    public double Value { get; set; }
    public string UnitName { get; set; } = "";
}
