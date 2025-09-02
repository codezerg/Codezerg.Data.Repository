using Codezerg.Data.Repository.Example.Models;
using Codezerg.Data.Repository;
using System.Diagnostics;

namespace Codezerg.Data.Repository.Example.Examples;

/// <summary>
/// Demonstrates usage of CachedRepository with performance benefits
/// </summary>
public class CachedExample
{
    public static void Run()
    {
        Console.WriteLine("\n=== Cached Repository Example ===\n");
        
        // Create Data directory if it doesn't exist
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        
        // Connection string for SQLite
        var connectionString = $"Data Source={Path.Combine(dataDir, "CachedExample.db")}";
        var repository = new CachedRepository<Order>("Microsoft.Data.Sqlite", connectionString);
        
        // Clear existing data for demo
        var existing = repository.GetAll().ToList();
        if (existing.Any())
        {
            repository.DeleteRange(existing);
        }
        
        // Add orders
        Console.WriteLine("Adding orders...");
        var order1 = new Order
        {
            CustomerId = 1,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 1599.97m,
            Status = OrderStatus.Processing,
            ShippingAddress = "123 Main St, Anytown, USA",
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Quantity = 1, UnitPrice = 1299.99m },
                new() { ProductId = 2, Quantity = 2, UnitPrice = 149.99m }
            }
        };
        repository.InsertWithIdentity(order1);
        Console.WriteLine($"Added: {order1}");
        
        var order2 = new Order
        {
            CustomerId = 2,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            TotalAmount = 239.97m,
            Status = OrderStatus.Delivered,
            ShippingAddress = "456 Oak Ave, Another City, USA",
            Items = new List<OrderItem>
            {
                new() { ProductId = 2, Quantity = 3, UnitPrice = 79.99m }
            }
        };
        repository.InsertWithIdentity(order2);
        Console.WriteLine($"Added: {order2}");
        
        // Demonstrate cache performance
        Console.WriteLine("\n--- Performance Comparison ---");
        
        var sw = new Stopwatch();
        
        // First read (potentially from database if not cached)
        sw.Start();
        var firstRead = repository.FirstOrDefault(o => o.Id == order1.Id);
        sw.Stop();
        var firstReadTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"First read: {firstReadTime}ms");
        
        // Second read (from cache)
        sw.Restart();
        var secondRead = repository.FirstOrDefault(o => o.Id == order1.Id);
        sw.Stop();
        var secondReadTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Second read (from cache): {secondReadTime}ms");
        
        if (firstReadTime > 0 && secondReadTime > 0)
        {
            Console.WriteLine($"Cache speedup: {(double)firstReadTime / secondReadTime:F1}x faster");
        }
        
        // Bulk read performance
        Console.WriteLine("\nBulk read performance (100 iterations):");
        
        sw.Restart();
        for (int i = 0; i < 100; i++)
        {
            var orders = repository.GetAll().ToList();
        }
        sw.Stop();
        Console.WriteLine($"100 GetAll operations: {sw.ElapsedMilliseconds}ms (avg: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
        
        // Update and cache invalidation
        Console.WriteLine("\nUpdating order status...");
        order1.Status = OrderStatus.Shipped;
        repository.Update(order1);
        
        var updated = repository.FirstOrDefault(o => o.Id == order1.Id);
        Console.WriteLine($"Updated: {updated}");
        
        // Query operations
        Console.WriteLine("\nQuerying orders:");
        var processingOrders = repository.Find(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Shipped);
        foreach (var o in processingOrders)
        {
            Console.WriteLine($"  {o}");
        }
        
        // Thread safety demonstration
        Console.WriteLine("\n--- Thread Safety Test ---");
        var tasks = new List<Task>();
        var random = new Random();
        
        // Create 10 concurrent tasks
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(() =>
            {
                var order = new Order
                {
                    CustomerId = taskId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = (decimal)(random.Next(100, 1000) + random.NextDouble()),
                    Status = (OrderStatus)random.Next(0, 5),
                    ShippingAddress = $"Address for Task {taskId}"
                };
                
                repository.InsertWithIdentity(order);
                Console.WriteLine($"  Task {taskId}: Added order {order.Id}");
            }));
        }
        
        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("All concurrent operations completed successfully!");
        
        // Final count
        var totalOrders = repository.Count();
        Console.WriteLine($"\nTotal orders in repository: {totalOrders}");
        
        // Persistence verification
        Console.WriteLine("\nReloading repository to verify persistence...");
        var newRepository = new CachedRepository<Order>("Microsoft.Data.Sqlite", connectionString);
        var persistedCount = newRepository.Count();
        Console.WriteLine($"Orders persisted to database: {persistedCount}");
    }
}