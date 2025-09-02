using LinqToDB.Mapping;

namespace Codezerg.Data.Repository.Example.Models;

/// <summary>
/// Example customer entity with auto-incrementing ID
/// </summary>
[Table("Customers")]
public class Customer
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public DateTime RegisteredAt { get; set; }
    
    public bool IsActive { get; set; }
    
    public override string ToString()
    {
        return $"Customer #{Id}: {FirstName} {LastName} ({Email})";
    }
}