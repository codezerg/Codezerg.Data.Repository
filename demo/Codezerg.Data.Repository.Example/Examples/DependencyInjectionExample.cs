using System;
using System.IO;
using System.Linq;
using Codezerg.Data.Repository;
using Codezerg.Data.Repository.Example.Models;

namespace Codezerg.Data.Repository.Example.Examples;

/// <summary>
/// Demonstrates manual repository creation and usage (DI infrastructure was removed)
/// </summary>
public class DependencyInjectionExample
{
    public static void Run()
    {
            Console.WriteLine("\n=== Repository Usage Example (Manual Creation) ===\n");
            
            // Create repositories manually
            var dbPath = Path.Combine(Path.GetTempPath(), "example_di.db");
            var connectionString = $"Data Source={dbPath}";
            
            // Use CachedRepository for better performance
            var productRepository = new Repository<Product>(RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString));
            var orderRepository = new Repository<Order>(RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString));
            var orderItemRepository = new Repository<OrderItem>(RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString));
            
            try
            {
                // Use services with manually created repositories
                Console.WriteLine("Using ProductService with repository...");
                var productService = new ProductService(productRepository);
                productService.RunDemo();
                
                Console.WriteLine("\nUsing OrderService with repositories...");
                var orderService = new OrderService(orderRepository, orderItemRepository);
                orderService.RunDemo();
                
                // Demonstrate shared repository behavior
                Console.WriteLine("\n--- Repository State Verification ---");
                
                // Create another instance pointing to same database
                var productRepository2 = new Repository<Product>(RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString));
                
                var count1 = productRepository.Count();
                var count2 = productRepository2.Count();
                
                Console.WriteLine($"Repository 1 count: {count1}");
                Console.WriteLine($"Repository 2 count: {count2}");
                Console.WriteLine($"Same instance: {ReferenceEquals(productRepository, productRepository2)}");
                Console.WriteLine("Note: Both repositories show the same count as they use the same database");
                
                productRepository2.Dispose();
            }
            finally
            {
                // Clean up
                productRepository.Dispose();
                orderRepository.Dispose();
                orderItemRepository.Dispose();
                
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }
    }

    /// <summary>
    /// Example service using repository
    /// </summary>
    public class ProductService
    {
        private readonly IRepository<Product> _repository;
        
        public ProductService(IRepository<Product> repository)
        {
            _repository = repository;
        }
        
        public void RunDemo()
        {
            Console.WriteLine("Starting product service demo");
            
            // Clear existing products
            var existing = _repository.GetAll().ToList();
            if (existing.Any())
            {
                _repository.DeleteRange(existing);
            }
            
            // Add sample products
            var products = new[]
            {
                new Product { Name = "Laptop", Price = 1299.99m, StockQuantity = 5, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Monitor", Price = 399.99m, StockQuantity = 10, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Keyboard", Price = 79.99m, StockQuantity = 20, CreatedAt = DateTime.UtcNow }
            };
            
            foreach (var product in products)
            {
                _repository.Insert(product);
                Console.WriteLine($"  Added product: {product.Name} at ${product.Price}");
            }
            
            // Calculate inventory value
            var allProducts = _repository.GetAll();
            var totalValue = allProducts.Sum(p => p.Price * p.StockQuantity);
            
            Console.WriteLine($"  Total inventory value: ${totalValue:F2}");
        }
    }

    /// <summary>
    /// Example order service using repositories
    /// </summary>
    public class OrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        
        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository)
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
        }
        
        public void RunDemo()
        {
            Console.WriteLine("Starting order service demo");
            
            // Clear existing data
            var existingOrders = _orderRepository.GetAll().ToList();
            if (existingOrders.Any())
            {
                _orderRepository.DeleteRange(existingOrders);
            }
            
            var existingItems = _orderItemRepository.GetAll().ToList();
            if (existingItems.Any())
            {
                _orderItemRepository.DeleteRange(existingItems);
            }
            
            // Create order with items
            var order = new Order
            {
                CustomerId = 1,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                ShippingAddress = "123 Main St",
                TotalAmount = 0
            };
            
            _orderRepository.Insert(order);
            Console.WriteLine($"  Created order {order.Id}");

            // Add order items
            var items = new[]
            {
                new OrderItem { OrderId = order.Id, ProductId = 1, Quantity = 2, UnitPrice = 50.00m },
                new OrderItem { OrderId = order.Id, ProductId = 2, Quantity = 1, UnitPrice = 100.00m }
            };

            decimal total = 0;
            foreach (var item in items)
            {
                _orderItemRepository.Insert(item);
                total += item.Subtotal;
                Console.WriteLine($"  Added item: Product {item.ProductId} x{item.Quantity}");
            }
            
            // Update order total
            order.TotalAmount = total;
            _orderRepository.Update(order);
            
            Console.WriteLine($"  Order {order.Id} total: ${total:F2}");
            
            // Query order items
            var orderItems = _orderItemRepository.Find(i => i.OrderId == order.Id);
            Console.WriteLine($"  Found {orderItems.Count()} items for order {order.Id}");
    }
}