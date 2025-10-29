namespace Hefi.Api.Dtos;


// represents a single food item included in a meal creation request.
public record MealItemCreate(
    string FoodLabel,
    double Grams,
    int Kcal,
    double Protein,
    double Carbs,
    double Fat,
    double Sugar
);

// represents a persisted food item entry in the database, belonging to a meal.
public class MealItemDto
{
    public int Id { get; set; }
    public string FoodLabel { get; set; } = "";
    public double Grams { get; set; }
    public int Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }
}

// represents the request payload for creating a meal record.
public record MealCreate(
    DateTime? EatenAt,
    int? TotalKcal,
    double? TotalProtein,
    double? TotalCarbs,
    double? TotalFat,
    double? TotalSugar,
    List<MealItemCreate> Items
);

// represents a complete meal record with nutritional totals and individual items.
public record MealDto(
    int Id,
    int UserId,
    DateTime EatenAt,
    int TotalKcal,
    double TotalProtein,
    double TotalCarbs,
    double TotalFat,
    double TotalSugar,
    List<MealItemDto> Items
);

// represents a simplified meal header used for summaries or listings.
public class MealHeader
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime EatenAt { get; set; }
    public int TotalKcal { get; set; }
    public double TotalProtein { get; set; }
    public double TotalCarbs { get; set; }
    public double TotalFat { get; set; }
    public double TotalSugar { get; set; }
}
