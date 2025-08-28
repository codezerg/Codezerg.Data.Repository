using Codezerg.Data.Repository.Example.Models;
using Codezerg.Data.Repository;

namespace Codezerg.Data.Repository.Example.Examples;

/// <summary>
/// Demonstrates usage of DatabaseRepository with SQLite
/// </summary>
public class DatabaseExample
{
    public static void Run()
    {
        Console.WriteLine("\n=== Database Repository Example (SQLite) ===\n");
        
        // Create Data directory if it doesn't exist
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        
        // Connection string for SQLite
        var connectionString = $"Data Source={Path.Combine(dataDir, "ExampleDatabase.db")}";
        var repository = new DatabaseRepository<Customer>("Microsoft.Data.Sqlite", connectionString);
        
        // Clear existing data for demo
        var existing = repository.GetAll().ToList();
        if (existing.Any())
        {
            repository.DeleteRange(existing);
        }
        
        // Add customers
        Console.WriteLine("Adding customers to database...");
        var customer1 = new Customer
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "555-0100",
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };
        repository.InsertWithIdentity(customer1);
        Console.WriteLine($"Added: {customer1}");
        
        var customer2 = new Customer
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Phone = "555-0101",
            RegisteredAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };
        repository.InsertWithIdentity(customer2);
        Console.WriteLine($"Added: {customer2}");
        
        var customer3 = new Customer
        {
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob.j@example.com",
            Phone = "555-0102",
            RegisteredAt = DateTime.UtcNow.AddDays(-60),
            IsActive = false
        };
        repository.InsertWithIdentity(customer3);
        Console.WriteLine($"Added: {customer3}");
        
        // Query customers
        Console.WriteLine("\nActive customers:");
        var activeCustomers = repository.Find(c => c.IsActive);
        foreach (var c in activeCustomers)
        {
            Console.WriteLine($"  {c}");
        }
        
        // Update customer
        Console.WriteLine("\nActivating Bob Johnson...");
        customer3.IsActive = true;
        repository.Update(customer3);
        
        var updated = repository.FirstOrDefault(c => c.Id == customer3.Id);
        Console.WriteLine($"Updated: {updated} (Active: {updated!.IsActive})");
        
        // Batch operations
        Console.WriteLine("\nAdding multiple customers in batch...");
        var newCustomers = new[]
        {
            new Customer
            {
                FirstName = "Alice",
                LastName = "Brown",
                Email = "alice.b@example.com",
                Phone = "555-0103",
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            },
            new Customer
            {
                FirstName = "Charlie",
                LastName = "Davis",
                Email = "charlie.d@example.com",
                Phone = "555-0104",
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            }
        };
        
        foreach (var customer in newCustomers)
        {
            repository.InsertWithIdentity(customer);
        }
        Console.WriteLine($"Added {newCustomers.Length} customers in batch");
        
        // Complex query
        Console.WriteLine("\nCustomers registered in the last 31 days:");
        var recentCustomers = repository.Find(c => 
            c.RegisteredAt > DateTime.UtcNow.AddDays(-31) && c.IsActive);
        foreach (var c in recentCustomers)
        {
            Console.WriteLine($"  {c} - Registered: {c.RegisteredAt:yyyy-MM-dd}");
        }
        
        // Count
        var totalCount = repository.Count();
        var activeCount = repository.Count(c => c.IsActive);
        Console.WriteLine($"\nTotal customers: {totalCount}");
        Console.WriteLine($"Active customers: {activeCount}");
        
        // Persistence check
        Console.WriteLine("\nData is persisted in SQLite database at:");
        Console.WriteLine($"  {Path.Combine(dataDir, "ExampleDatabase.db")}");
    }
}