using LinqToDB.Mapping;

namespace Codezerg.Data.Repository.Example.Models;

/// <summary>
/// Example product entity with auto-incrementing ID
/// </summary>
[Table("Products")]
public class Product
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    public int StockQuantity { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public override string ToString()
    {
        return $"Product #{Id}: {Name} - ${Price:F2} (Stock: {StockQuantity})";
    }
}