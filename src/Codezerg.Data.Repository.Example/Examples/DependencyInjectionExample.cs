using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Example.Models;
using Codezerg.Data.Repository.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Codezerg.Data.Repository.Example.Examples;

/// <summary>
/// Demonstrates dependency injection setup and usage
/// </summary>
public class DependencyInjectionExample
{
    public static void Run()
    {
        Console.WriteLine("\n=== Dependency Injection Example ===\n");
        
        // Setup DI container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Add repository services
        services.AddRepositoryServices();
        
        // Add application services
        services.AddTransient<ProductService>();
        services.AddTransient<OrderService>();
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Use services
        Console.WriteLine("Using ProductService with injected repository...");
        var productService = serviceProvider.GetRequiredService<ProductService>();
        productService.RunDemo();
        
        Console.WriteLine("\nUsing OrderService with injected repository...");
        var orderService = serviceProvider.GetRequiredService<OrderService>();
        orderService.RunDemo();
        
        // Demonstrate singleton behavior
        Console.WriteLine("\n--- Singleton Verification ---");
        var repo1 = serviceProvider.GetRequiredService<IRepository<Product>>();
        var repo2 = serviceProvider.GetRequiredService<IRepository<Product>>();
        
        repo1.InsertWithIdentity(new Product
        {
            Name = "Test Product",
            Price = 99.99m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        });
        
        var count1 = repo1.Count();
        var count2 = repo2.Count();
        
        Console.WriteLine($"Repository 1 count: {count1}");
        Console.WriteLine($"Repository 2 count: {count2}");
        Console.WriteLine($"Same instance: {ReferenceEquals(repo1, repo2)}");
        Console.WriteLine("Note: Both repositories show the same count, confirming singleton behavior");
    }
}

/// <summary>
/// Example service using injected repository
/// </summary>
public class ProductService
{
    private readonly IRepository<Product> _repository;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IRepository<Product> repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public void RunDemo()
    {
        _logger.LogInformation("Starting product service demo");
        
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
            _repository.InsertWithIdentity(product);
            _logger.LogInformation("Added product: {Name} at ${Price}", product.Name, product.Price);
        }
        
        // Calculate inventory value
        var allProducts = _repository.GetAll();
        var totalValue = allProducts.Sum(p => p.Price * p.StockQuantity);
        
        _logger.LogInformation("Total inventory value: ${TotalValue:F2}", totalValue);
    }
}

/// <summary>
/// Example order service using injected repositories
/// </summary>
public class OrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _logger = logger;
    }
    
    public void RunDemo()
    {
        _logger.LogInformation("Starting order service demo");
        
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
        
        _orderRepository.InsertWithIdentity(order);
        _logger.LogInformation("Created order {OrderId}", order.Id);
        
        // Add order items
        var items = new[]
        {
            new OrderItem { OrderId = order.Id, ProductId = 1, Quantity = 2, UnitPrice = 50.00m },
            new OrderItem { OrderId = order.Id, ProductId = 2, Quantity = 1, UnitPrice = 100.00m }
        };
        
        decimal total = 0;
        foreach (var item in items)
        {
            _orderItemRepository.InsertWithIdentity(item);
            total += item.Subtotal;
            _logger.LogInformation("Added item: Product {ProductId} x{Quantity}", item.ProductId, item.Quantity);
        }
        
        // Update order total
        order.TotalAmount = total;
        _orderRepository.Update(order);
        
        _logger.LogInformation("Order {OrderId} total: ${Total:F2}", order.Id, total);
        
        // Query order items
        var orderItems = _orderItemRepository.Find(i => i.OrderId == order.Id);
        _logger.LogInformation("Found {Count} items for order {OrderId}", orderItems.Count(), order.Id);
    }
}