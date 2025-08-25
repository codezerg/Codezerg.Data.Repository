# Codezerg.Data.Repository

A flexible .NET 8 repository pattern library built on top of [linq2db](https://github.com/linq2db/linq2db) that provides three implementation strategies with a unified `IRepository<T>` interface. Designed for simplicity, performance, and thread safety.

## Features

- 🚀 **Three Repository Strategies**
  - **InMemoryRepository**: Pure in-memory storage with deep copy protection
  - **DatabaseRepository**: Direct database access using linq2db  
  - **CachedRepository**: Hybrid approach combining in-memory caching with database persistence

- 🔒 **Thread Safety**
  - InMemoryRepository uses `ReaderWriterLockSlim` for concurrent access
  - CachedRepository fully thread-safe with read/write locks
  - Safe for use in multi-threaded applications and ASP.NET Core

- 🎯 **Automatic Property Mapping**
  - **No attributes required!** All public properties with get/set are automatically mapped
  - Supports standard .NET DataAnnotations attributes
  - Automatically translates `[Key]`, `[Required]`, `[MaxLength]`, etc. to linq2db equivalents
  - Falls back to linq2db attributes for advanced scenarios

- ⚙️ **Attribute-Based Configuration**
  - Configure repositories using attributes on entity classes
  - Support for runtime configuration overrides
  - Fluent configuration API

- 🗄️ **Database Support via linq2db**
  - SQLite (default)
  - SQL Server (via linq2db.SqlServer)
  - PostgreSQL (via linq2db.PostgreSQL)
  - MySQL (via linq2db.MySql)
  - 30+ other providers supported by linq2db

## Installation

```bash
dotnet add package Codezerg.Data.Repository
```

## Quick Start

### 1. Define Your Entity

```csharp
using LinqToDB.Mapping;

[Table("Products")]
public class Product
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    // No [Column] attribute needed - automatically mapped!
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

Or use standard .NET attributes:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(500)]
    public string Description { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Configure Services

```csharp
// Program.cs or Startup.cs
builder.Services.AddRepositoryServicesWithAttributes(options =>
{
    options.ProviderName = LinqToDB.ProviderName.SQLite;
    options.DataPath = "Data";
    options.UseCachedRepository = true; // Default strategy
});
```

### 3. Use the Repository

```csharp
public class ProductService
{
    private readonly IRepository<Product> _repository;
    
    public ProductService(IRepository<Product> repository)
    {
        _repository = repository;
    }
    
    public async Task<Product> CreateProduct(string name, decimal price)
    {
        var product = new Product 
        { 
            Name = name, 
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
        
        var id = _repository.InsertWithIdentity(product);
        return product;
    }
    
    public IEnumerable<Product> GetProducts(decimal minPrice)
    {
        return _repository.Find(p => p.Price >= minPrice);
    }
}
```

## Repository Strategies

### InMemoryRepository
Perfect for testing and temporary data storage:

```csharp
[InMemoryRepository(PersistAcrossSessions = true)]
public class SessionData
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Data { get; set; }
}
```

### DatabaseRepository
Direct database access without caching:

```csharp
[DatabaseRepository(
    DatabaseName = "MyApp",
    EnableWalMode = true,
    AutoCreateTable = true
)]
public class AuditLog
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### CachedRepository
Best of both worlds - in-memory performance with database persistence:

```csharp
[CachedRepository(
    PreloadCache = true,
    DatabaseName = "MyApp"
)]
public class Configuration
{
    [PrimaryKey]
    public string Key { get; set; }
    public string Value { get; set; }
}
```

## Advanced Features

### Runtime Configuration Overrides

```csharp
// Temporarily override configuration
using (var scope = serviceProvider.CreateScopedRepositoryContext())
{
    scope.Override<Product>(config =>
    {
        config.Strategy = RepositoryStrategy.InMemory;
        config.EnableLogging = true;
    });
    
    // Repository uses overridden configuration within this scope
    var products = repository.GetAll();
} // Original configuration restored
```

### Fluent Configuration

```csharp
services.ConfigureRepositoryOverrides(builder =>
{
    builder
        .ForEntity<Product>()
        .UseInMemory()
        .EnableLogging()
        .Apply();
});
```

### Bulk Operations

```csharp
// Efficient bulk insert using database-specific optimizations
var products = GenerateProducts(10000);
var count = repository.InsertRange(products);
```

## Thread Safety

All repository implementations are thread-safe:

```csharp
// Safe for concurrent operations
var tasks = new List<Task>();

// Multiple readers
for (int i = 0; i < 10; i++)
{
    tasks.Add(Task.Run(() => 
    {
        var products = repository.GetAll();
    }));
}

// Multiple writers
for (int i = 0; i < 5; i++)
{
    tasks.Add(Task.Run(() => 
    {
        repository.Insert(new Product { Name = $"Product {i}" });
    }));
}

await Task.WhenAll(tasks);
```

## Database Provider Support

The library uses linq2db and supports all its providers. Simply install the appropriate package and configure:

### SQL Server
```bash
dotnet add package linq2db.SqlServer
```

```csharp
options.ProviderName = LinqToDB.ProviderName.SqlServer2019;
options.ConnectionStringTemplate = "Server={Server};Database={Database};Trusted_Connection=true;";
```

### PostgreSQL
```bash
dotnet add package linq2db.PostgreSQL
```

```csharp
options.ProviderName = LinqToDB.ProviderName.PostgreSQL15;
options.ConnectionStringTemplate = "Host={Server};Database={Database};Username={User};Password={Password}";
```

### MySQL
```bash
dotnet add package linq2db.MySql
```

```csharp
options.ProviderName = LinqToDB.ProviderName.MySql80;
options.ConnectionStringTemplate = "Server={Server};Database={Database};Uid={User};Pwd={Password}";
```

## Important Notes

### This Library is NOT for:
- ❌ Database migrations or schema management
- ❌ Complex ORM features (use EF Core for that)
- ❌ Database administration tasks

### This Library IS for:
- ✅ Simple, fast repository pattern implementation
- ✅ Caching and in-memory data strategies
- ✅ Thread-safe data access
- ✅ Clean abstraction over linq2db

### Entity Guidelines

1. **Automatic Property Mapping**: All public properties with getters and setters are automatically mapped as columns - no `[Column]` attribute needed!
2. **Standard .NET Attributes Support**: Use familiar DataAnnotations attributes which are automatically translated to linq2db:
   - `[Key]` → `[PrimaryKey]`
   - `[Required]` → NOT NULL constraint
   - `[MaxLength]`, `[StringLength]` → Column length
   - `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` → `[Identity]`
   - `[NotMapped]` → Excludes property from mapping
   - `[Table]`, `[Column]` → Table and column naming
3. **Use linq2db attributes** for advanced mapping (`[PrimaryKey]`, `[Identity]`, `[Column]`, etc.) - these take precedence over automatic mapping
4. **Use `int` for Identity columns with SQLite** (SQLite AUTOINCREMENT only works with INTEGER)
5. **Let linq2db handle table creation** with all mapped properties (or use proper migration tools for production)

## Architecture

```
┌─────────────────────────────────────────────┐
│           Application Layer                 │
└─────────────┬───────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────┐
│         IRepository<T> Interface            │
└─────────────┬───────────────────────────────┘
              │
      ┌───────┴───────┬──────────────┐
      ▼               ▼              ▼
┌──────────┐   ┌──────────┐   ┌──────────┐
│InMemory  │   │Database  │   │ Cached   │
│Repository│   │Repository│   │Repository│
└──────────┘   └────┬─────┘   └────┬─────┘
                    │               │
                    ▼               ▼
              ┌──────────┐   ┌──────────┐
              │ linq2db  │   │ linq2db  │
              └────┬─────┘   └────┬─────┘
                   │               │
                   ▼               ▼
              ┌──────────┐   ┌──────────┐
              │   SQLite │   │   Cache  │
              │    DB    │   │ + SQLite │
              └──────────┘   └──────────┘
```

## Testing

The library includes comprehensive tests demonstrating:
- Thread safety under concurrent load
- Attribute-based configuration
- Runtime configuration overrides
- All three repository strategies
- Bulk operations

Run tests:
```bash
dotnet test
```

## Performance Considerations

- **InMemoryRepository**: Fastest for small datasets, no persistence
- **DatabaseRepository**: Direct database access, good for large datasets
- **CachedRepository**: Best for read-heavy workloads with moderate write frequency
- **Bulk operations**: Automatically use provider-specific optimizations (SqlBulkCopy for SQL Server, etc.)

## Contributing

Contributions are welcome! Please ensure:
1. All tests pass
2. Code follows existing patterns
3. Thread safety is maintained
4. No custom SQL generation (use linq2db)

## License

MIT License - see LICENSE file for details

## Dependencies

- [linq2db](https://github.com/linq2db/linq2db) (v5.4.1) - LINQ to database provider
- [Microsoft.Data.Sqlite](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/) (v9.0.1) - SQLite provider
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) (v8.0.0) - DI abstractions

## Support

- Create an issue on GitHub for bugs or feature requests
- See [CLAUDE.md](CLAUDE.md) for AI assistant integration
- Check [MISSING_IMPLEMENTATIONS.md](MISSING_IMPLEMENTATIONS.md) for known limitations
- Review [SQL_SERVER_SUPPORT_SIMPLIFIED.md](SQL_SERVER_SUPPORT_SIMPLIFIED.md) for database provider setup

## Version History

### 1.0.0
- Initial release with three repository strategies
- Thread-safe implementations
- Attribute-based configuration
- Runtime configuration overrides
- linq2db integration for all database operations