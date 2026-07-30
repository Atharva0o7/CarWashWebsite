namespace CarWashWebsite.Models;

/// <summary>Feeds the "select your car" picker. Table: <c>car_brands</c>.</summary>
public class CarBrand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public List<CarModel> Models { get; set; } = [];
}

/// <summary>Table: <c>car_models</c>. Body type drives the price multiplier.</summary>
public class CarModel
{
    public int Id { get; set; }
    public int CarBrandId { get; set; }
    public CarBrand? Brand { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Hatchback, Sedan, SUV or Luxury.</summary>
    public string BodyType { get; set; } = "";

    public int SortOrder { get; set; }
}
