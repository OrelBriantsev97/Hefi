namespace Hefi.Mobile.Models;


// represents a single food item included when creating a new meal.
public class MealItemCreate
{
    public string FoodLabel { get; set; } = "";
    public double Amount { get; set; }    
    public string Unit { get; set; } = "";
    public int Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }
}


// represents a food item as stored in the backend and returned in meal responses.
public class MealItemDto
{
    public int Id { get; set; }
    public string FoodLabel { get; set; } = "";
    public double Amount { get; set; }  
    public string Unit { get; set; } = "";
    public int Kcal { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }
}

// represents a complete meal record
public class MealDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime EatenAt { get; set; }

    public int TotalKcal { get; set; }

    public double TotalProtein { get; set; }
    public double TotalCarbs { get; set; }
    public double TotalFat { get; set; }
    public double TotalSugar { get; set; }

    public List<MealItemDto> Items { get; set; } = new();
}
