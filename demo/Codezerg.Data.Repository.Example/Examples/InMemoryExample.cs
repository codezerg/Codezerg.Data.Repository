using Codezerg.Data.Repository.Example.Models;
using Codezerg.Data.Repository;

namespace Codezerg.Data.Repository.Example.Examples;

/// <summary>
/// Demonstrates usage of InMemoryRepository
/// </summary>
public class InMemoryExample
{
    public static void Run()
    {
        Console.WriteLine("\n=== InMemory Repository Example ===\n");
        
        var repository = new Repository<Product>();
        
        // Add products
        Console.WriteLine("Adding products...");
        var product1 = new Product
        {
            Name = "Laptop",
            Description = "High-performance laptop",
            Price = 1299.99m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };
        repository.Insert(product1);
        Console.WriteLine($"Added: {product1}");
        
        var product2 = new Product
        {
            Name = "Mouse",
            Description = "Wireless gaming mouse",
            Price = 79.99m,
            StockQuantity = 50,
            CreatedAt = DateTime.UtcNow
        };
        repository.Insert(product2);
        Console.WriteLine($"Added: {product2}");
        
        var product3 = new Product
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 159.99m,
            StockQuantity = 25,
            CreatedAt = DateTime.UtcNow
        };
        repository.Insert(product3);
        Console.WriteLine($"Added: {product3}");
        
        // Get all products
        Console.WriteLine("\nAll products:");
        var allProducts = repository.GetAll();
        foreach (var p in allProducts)
        {
            Console.WriteLine($"  {p}");
        }
        
        // Find by predicate
        Console.WriteLine("\nProducts under $100:");
        var cheapProducts = repository.Find(p => p.Price < 100);
        foreach (var p in cheapProducts)
        {
            Console.WriteLine($"  {p}");
        }
        
        // Update product
        Console.WriteLine("\nUpdating mouse price...");
        product2.Price = 69.99m;
        product2.UpdatedAt = DateTime.UtcNow;
        repository.Update(product2);
        
        var updated = repository.FirstOrDefault(p => p.Id == product2.Id);
        Console.WriteLine($"Updated: {updated}");
        
        // Demonstrate deep copy protection
        Console.WriteLine("\nDemonstrating deep copy protection:");
        var retrieved = repository.FirstOrDefault(p => p.Id == product1.Id);
        Console.WriteLine($"Original price: ${retrieved!.Price}");
        
        retrieved.Price = 999.99m; // This won't affect the repository
        var retrievedAgain = repository.FirstOrDefault(p => p.Id == product1.Id);
        Console.WriteLine($"Price after external modification: ${retrievedAgain!.Price}");
        Console.WriteLine("Note: Price remains unchanged due to deep copy protection");
        
        // Delete product
        Console.WriteLine($"\nDeleting product {product3.Id}...");
        repository.Delete(product3);
        
        Console.WriteLine("Remaining products:");
        allProducts = repository.GetAll();
        foreach (var p in allProducts)
        {
            Console.WriteLine($"  {p}");
        }
        
        // Count
        var count = repository.Count();
        Console.WriteLine($"\nTotal products: {count}");
        
        var expensiveCount = repository.Count(p => p.Price > 100);
        Console.WriteLine($"Expensive products (>$100): {expensiveCount}");
    }
}