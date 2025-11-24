using System;
using System.IO;
using System.Linq;
using Codezerg.Data.Repository.Example.Models;

namespace Codezerg.Data.Repository.Example.Examples
{
    public static class UnifiedRepositoryExample
    {
        public static void Run()
        {
            Console.WriteLine("=== Unified Repository Example ===\n");

            // Example 1: In-Memory Mode
            Console.WriteLine("1. In-Memory Mode - Fast, temporary storage");
            Console.WriteLine("   Perfect for: Unit testing, temporary data, development\n");
            RunInMemoryExample();

            // Example 2: Database Mode
            Console.WriteLine("\n2. Database Mode - Direct database access");
            Console.WriteLine("   Perfect for: Write-heavy scenarios, minimal memory usage\n");
            RunDatabaseExample();

            // Example 3: Cached Mode
            Console.WriteLine("\n3. Cached Mode - In-memory cache with database persistence");
            Console.WriteLine("   Perfect for: Read-heavy scenarios, optimal query performance\n");
            RunCachedExample();

            // Example 4: Switching between modes
            Console.WriteLine("\n4. Easy Mode Switching - Change storage strategy without code changes\n");
            DemonstrateModeSwitching();
        }

        private static void RunInMemoryExample()
        {
            // Create repository with in-memory storage
            var options = RepositoryOptions.InMemory();
            using var repository = new Repository<Product>(options);

            // Insert some products
            repository.Insert(new Product { Name = "Laptop", Price = 999.99m, StockQuantity = 10 });
            repository.Insert(new Product { Name = "Mouse", Price = 29.99m, StockQuantity = 50 });
            repository.Insert(new Product { Name = "Keyboard", Price = 79.99m, StockQuantity = 30 });

            // Query products
            var expensiveProducts = repository.Find(p => p.Price > 50).ToList();
            Console.WriteLine($"   Found {expensiveProducts.Count} products over $50");

            // Update a product
            var laptop = repository.FirstOrDefault(p => p.Name == "Laptop");
            if (laptop != null)
            {
                laptop.Price = 899.99m;
                repository.Update(laptop);
                Console.WriteLine($"   Updated laptop price to ${laptop.Price}");
            }

            // Display all products
            Console.WriteLine($"   Total products in memory: {repository.Count()}");
        }

        private static void RunDatabaseExample()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unified_database.db");

            // Create repository with database storage
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", $"Data Source={dbPath}");
            using var repository = new Repository<Product>(options);

            // Clean up previous data
            repository.DeleteMany(p => true);

            // Insert products
            repository.Insert(new Product { Name = "Monitor", Price = 299.99m, StockQuantity = 15 });
            repository.Insert(new Product { Name = "Webcam", Price = 89.99m, StockQuantity = 25 });

            // Query from database
            var allProducts = repository.GetAll().ToList();
            Console.WriteLine($"   Products in database: {allProducts.Count}");

            foreach (var product in allProducts)
            {
                Console.WriteLine($"   - {product.Name}: ${product.Price} (Stock: {product.StockQuantity})");
            }

            Console.WriteLine($"   Database file: {dbPath}");
        }

        private static void RunCachedExample()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unified_cached.db");

            // Create repository with cached storage
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", $"Data Source={dbPath}");
            using var repository = new Repository<Product>(options);

            // Clean up previous data
            repository.DeleteMany(p => true);

            // Insert products (writes to database and cache)
            repository.Insert(new Product { Name = "Headphones", Price = 149.99m, StockQuantity = 40 });
            repository.Insert(new Product { Name = "Microphone", Price = 199.99m, StockQuantity = 20 });

            // Query from cache (fast!)
            var cachedProducts = repository.GetAll().ToList();
            Console.WriteLine($"   Products in cache: {cachedProducts.Count}");

            // Create a second repository instance to demonstrate persistence
            using var repository2 = new Repository<Product>(options);
            var persistedProducts = repository2.GetAll().ToList();
            Console.WriteLine($"   Products persisted to database: {persistedProducts.Count}");

            foreach (var product in persistedProducts)
            {
                Console.WriteLine($"   - {product.Name}: ${product.Price}");
            }

            Console.WriteLine($"   Cache provides fast reads, database ensures persistence");
        }

        private static void DemonstrateModeSwitching()
        {
            // Demonstrate how easy it is to switch between modes
            Console.WriteLine("   You can easily switch storage modes by changing options:");
            Console.WriteLine();
            Console.WriteLine("   // In-Memory for testing");
            Console.WriteLine("   var options = RepositoryOptions.InMemory();");
            Console.WriteLine();
            Console.WriteLine("   // Database for production");
            Console.WriteLine("   var options = RepositoryOptions.Database(provider, connectionString);");
            Console.WriteLine();
            Console.WriteLine("   // Cached for high-performance reads");
            Console.WriteLine("   var options = RepositoryOptions.Cached(provider, connectionString);");
            Console.WriteLine();
            Console.WriteLine("   var repository = new Repository<Product>(options);");
        }
    }
}
