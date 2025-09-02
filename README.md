# Codezerg.Data.Repository

A simplified .NET repository pattern library built on top of [linq2db](https://github.com/linq2db/linq2db) that provides three implementation strategies with a unified `IRepository<T>` interface. Designed for simplicity, performance, and thread safety.

## Features

- 🚀 **Three Repository Strategies**
  - **InMemoryRepository**: Pure in-memory storage with deep copy protection
  - **DatabaseRepository**: Direct database access using linq2db  
  - **CachedRepository**: Hybrid approach combining in-memory caching with database persistence

- 🔒 **Thread Safety**
  - InMemoryRepository uses `ReaderWriterLockSlim` for concurrent access
  - DatabaseRepository thread-safe through connection-per-operation pattern
  - CachedRepository fully thread-safe with read/write locks

- 🎯 **Automatic Property Mapping**
  - All public properties with get/set are automatically mapped as database columns
  - Uses linq2db attributes for entity mapping configuration
  - Smart identity management with auto-incrementing primary keys

- 🗄️ **Database Support via linq2db**
  - SQLite (default)
  - SQL Server, PostgreSQL, MySQL, and 30+ other providers via linq2db

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
    
    // All properties are automatically mapped - no [Column] needed!
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Properties without attributes are automatically mapped using their names:

### 2. Create Repository

```csharp
// In-memory repository (great for testing)
var inMemoryRepo = new InMemoryRepository<Product>();

// Direct database repository
var dbRepo = new DatabaseRepository<Product>(
    LinqToDB.ProviderName.SQLite,
    "Data Source=products.db"
);

// Cached repository (best of both worlds)
var cachedRepo = new CachedRepository<Product>(
    LinqToDB.ProviderName.SQLite,
    "Data Source=products.db"
);
```

### 3. Use the Repository

```csharp
// Insert with auto-generated ID
var product = new Product 
{ 
    Name = "Widget",
    Price = 29.99m,
    CreatedAt = DateTime.UtcNow
};

int id = repository.InsertWithIdentity(product);
Console.WriteLine($"Created product with ID: {product.Id}");

// Query data
var expensiveProducts = repository.Find(p => p.Price > 50);
var productCount = repository.Count(p => p.StockQuantity > 0);

// Update
product.Price = 34.99m;
repository.Update(product);

// Delete
repository.Delete(product);
// or delete by condition
repository.DeleteMany(p => p.StockQuantity == 0);
```

## Repository Strategies

### InMemoryRepository

Perfect for testing and temporary data storage. Features deep copy protection to prevent external modifications.

```csharp
var repository = new InMemoryRepository<Product>();

// All data is stored in memory
// Thread-safe with ReaderWriterLockSlim
// Automatic identity generation
// Deep copy protection ensures data integrity
```

### DatabaseRepository  

Direct database access without caching. Best for large datasets or when data freshness is critical.

```csharp
var repository = new DatabaseRepository<Product>(
    LinqToDB.ProviderName.SQLite,
    "Data Source=myapp.db"
);

// Direct database operations
// Automatic table creation
// WAL mode enabled for SQLite
// Thread-safe through connection-per-operation
```

### CachedRepository

Combines in-memory performance with database persistence. Ideal for read-heavy workloads.

```csharp
var repository = new CachedRepository<Product>(
    LinqToDB.ProviderName.SQLite,
    "Data Source=myapp.db"
);

// In-memory cache for fast reads
// Automatic synchronization with database
// Thread-safe with ReaderWriterLockSlim
// Call Refresh() to reload from database
```

## IRepository Interface

All repositories implement the same interface:

```csharp
public interface IRepository<T>
{
    // Create
    int Insert(T entity);
    int InsertWithIdentity(T entity);
    long InsertWithInt64Identity(T entity);
    int InsertRange(IEnumerable<T> entities);

    // Read
    IEnumerable<T> GetAll();
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    T FirstOrDefault(Expression<Func<T, bool>> predicate);
    IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query);

    // Update
    int Update(T entity);
    int UpdateRange(IEnumerable<T> entities);

    // Delete
    int Delete(T entity);
    int DeleteRange(IEnumerable<T> entities);
    int DeleteMany(Expression<Func<T, bool>> predicate);

    // Count & Exists
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    bool Exists(Expression<Func<T, bool>> predicate);
}
```

## Advanced Features

### Complex Queries

Use the `Query` method for advanced LINQ operations:

```csharp
var results = repository.Query(q => 
    q.Where(p => p.Price > 10)
     .OrderBy(p => p.Name)
     .GroupBy(p => p.Category)
     .Select(g => new { Category = g.Key, Count = g.Count() })
);
```

### Bulk Operations

```csharp
var products = GenerateProducts(1000);
var insertedCount = repository.InsertRange(products);
var updatedCount = repository.UpdateRange(products);
var deletedCount = repository.DeleteRange(products);
```

### Thread Safety

All repositories are thread-safe:

```csharp
var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
{
    repository.Insert(new Product { Name = $"Product {i}" });
}));

await Task.WhenAll(tasks);
```

## Entity Mapping

The library uses smart entity mapping with these key components:

- **EntityMapping**: Handles database schema and table names
- **EntityOperations**: Manages entity manipulation and identity
- **EntityCloner**: Creates deep copies for data isolation
- **EntityMerger**: Updates entity values during operations
- **IdentityManager**: Auto-generates identity values
- **PrimaryKeyHelper**: Extracts and manages primary keys

### Automatic Mapping

All public properties with getters and setters are automatically mapped:

```csharp
public class Customer
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    // These are all automatically mapped
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Properties without setter are ignored
    public string FullName => $"{FirstName} {LastName}";
}
```

## Database Provider Support

### SQLite (Default)

```csharp
var repository = new DatabaseRepository<Product>(
    LinqToDB.ProviderName.SQLite,
    "Data Source=app.db"
);
```

### SQL Server

```bash
dotnet add package linq2db.SqlServer
```

```csharp
var repository = new DatabaseRepository<Product>(
    LinqToDB.ProviderName.SqlServer2019,
    "Server=localhost;Database=MyApp;Trusted_Connection=true;"
);
```

### PostgreSQL

```bash
dotnet add package linq2db.PostgreSQL
```

```csharp
var repository = new DatabaseRepository<Product>(
    LinqToDB.ProviderName.PostgreSQL15,
    "Host=localhost;Database=myapp;Username=user;Password=pass"
);
```

## Performance Considerations

- **InMemoryRepository**: Fastest for small datasets, no persistence
- **DatabaseRepository**: Direct database access, best for large datasets
- **CachedRepository**: Optimal for read-heavy workloads with moderate writes
- Deep copy operations in InMemoryRepository add overhead but ensure data integrity
- SQLite WAL mode enabled for better concurrent performance

## Testing

The library includes comprehensive unit tests:

```bash
dotnet test
```

Example test:

```csharp
[Test]
public void Repository_Should_Handle_Concurrent_Operations()
{
    var repository = new InMemoryRepository<TestEntity>();
    
    Parallel.For(0, 100, i =>
    {
        repository.Insert(new TestEntity { Name = $"Entity {i}" });
    });
    
    Assert.AreEqual(100, repository.Count());
}
```

## Important Notes

### This Library is NOT for:
- ❌ Database migrations or schema management
- ❌ Complex ORM features (use EF Core for that)
- ❌ Database administration tasks

### This Library IS for:
- ✅ Simple, fast repository pattern implementation
- ✅ Testing with in-memory repositories
- ✅ Caching strategies with database backing
- ✅ Thread-safe data access
- ✅ Clean abstraction over linq2db

## Architecture

```
Application Layer
        ↓
IRepository<T> Interface
        ↓
    ┌───┴───┬──────────┐
    ↓       ↓          ↓
InMemory  Database  Cached
    │       │          │
    │    linq2db    linq2db
    │       ↓          ↓
Memory   Database  Memory+DB
```

Key Components:
- **IRepository**: Unified interface for all implementations
- **InMemoryRepository**: Pure memory storage with thread safety
- **DatabaseRepository**: Direct database operations via linq2db
- **CachedRepository**: Hybrid approach with cache + persistence
- **EntityOperations**: Core entity manipulation logic
- **EntityMapping**: Database schema handling
- **IdentityManager**: Auto-incrementing ID management

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
- .NET Standard 2.0 / .NET 8.0 compatible

## Support

- Create an issue on GitHub for bugs or feature requests
- See [CLAUDE.md](CLAUDE.md) for AI assistant integration guidelines