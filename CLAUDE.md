# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
Role: Senior C# engineer focused on Simple, Lovable, Complete applications

## Build and Development Commands

```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore

# Create NuGet package
dotnet pack -c Release

# Run tests
dotnet test

# Run examples
dotnet run --project src/Codezerg.Data.Repository.Example
```

## Architecture Overview

This is a .NET Standard 2.0 repository pattern library providing three implementation strategies with a unified `IRepository<T>` interface:

1. **InMemoryRepository<T>**: Thread-safe in-memory storage with deep copy protection to prevent external modifications, uses ReaderWriterLockSlim for thread safety
2. **DatabaseRepository<T>**: Direct database access using linq2db with SQLite support, includes automatic table creation, thread-safe through connection-per-operation pattern
3. **CachedRepository<T>**: Hybrid approach combining in-memory caching with SQLite persistence, fully thread-safe using ReaderWriterLockSlim

### Key Architectural Components

- **IRepository<T>**: Core interface defining CRUD operations (src/Codezerg.Data.Repository/IRepository.cs:8)
- **EntityOperations<T>**: Handles entity manipulation, identity management, and deep copying (src/Codezerg.Data.Repository/EntityOperations.cs:13)
- **EntityMapping<T>**: Manages database mapping schemas and table names with automatic DataAnnotations to linq2db attribute mapping (src/Codezerg.Data.Repository/EntityMapping.cs:13)
- **PrimaryKeyHelper<T>**: Manages primary key detection and operations (src/Codezerg.Data.Repository/PrimaryKeyHelper.cs)
- **IdentityManager<T>**: Manages auto-incrementing identity values for entities (src/Codezerg.Data.Repository/IdentityManager.cs)
- **EntityCloner<T>**: Creates deep copies of entities for data isolation (src/Codezerg.Data.Repository/EntityCloner.cs)
- **EntityMerger<T>**: Updates entity properties while preserving primary keys (src/Codezerg.Data.Repository/EntityMerger.cs)

### Repository Selection Strategy

- All three repository types implement `IRepository<T>` interface
- InMemoryRepository: Best for unit testing and temporary data storage
- DatabaseRepository: Direct database operations with minimal overhead
- CachedRepository: Optimal for read-heavy scenarios with persistence requirements
- SQLite databases are stored relative to AppDomain.CurrentDomain.BaseDirectory
- Database names are derived from entity type via `EntityMapping<T>.GetDatabaseName()`

### Thread Safety

- **InMemoryRepository**: Fully thread-safe using ReaderWriterLockSlim with NoRecursion policy
- **DatabaseRepository**: Thread-safe through connection-per-operation pattern
- **CachedRepository**: Fully thread-safe using ReaderWriterLockSlim for coordinating memory and database operations

### DataAnnotations Support

The library automatically maps System.ComponentModel.DataAnnotations attributes to linq2db attributes:
- `[Key]` → `[PrimaryKey]`
- `[Required]` → `CanBeNull = false`
- `[MaxLength]` / `[StringLength]` → `Length`
- `[Column]` → Column name and type mapping
- `[Table]` → Table name
- `[NotMapped]` → `[NotColumn]`
- `[DatabaseGenerated(Identity)]` → `[Identity]`

### Package Dependencies

- **linq2db (3.7.0)**: ORM for database operations
- **Microsoft.Data.Sqlite (3.1.0)**: SQLite provider
- **Target Framework**: .NET Standard 2.0 (C# 7.3)

## Core Principles

**Code Quality:**
- Split files >300 lines, methods >30 lines
- Use XML docs for public APIs
- Ask before adding third-party packages
- Follow existing project structure and namespaces
- Treat compiler warnings as errors

**Development Philosophy:**
- Simplicity over cleverness
- Explicit over implicit
- Composition over inheritance
- Fail fast with clear error messages
- Write code a 14-year-old could understand
- Deep copy entities to prevent external modifications
- Use clear, descriptive names for classes and methods

## Working Modes

**Planner Mode:**
1. Ask 4-6 clarifying questions about scope and edge cases
2. Draft step-by-step plan
3. Get approval before implementing
4. Announce completion of each phase

**Architecture Mode:**
1. Ask strategic questions about scale, requirements, constraints
2. Provide tradeoff analysis with alternatives
3. Iterate on design based on feedback
4. Get approval for implementation plan

**Debug Mode:**
1. Identify 5-7 possible root causes
2. Narrow to 1-2 most likely culprits
3. Add targeted logging
4. Analyze findings comprehensively
5. Remove logs after approval

## Repository Interface Methods

### Create Operations
- `Insert(T entity)`: Insert single entity
- `InsertWithIdentity(T entity)`: Insert and return int identity
- `InsertWithInt64Identity(T entity)`: Insert and return long identity
- `InsertRange(IEnumerable<T> entities)`: Insert multiple entities

### Read Operations
- `GetAll()`: Retrieve all entities
- `Find(Expression<Func<T, bool>> predicate)`: Find entities by predicate
- `FirstOrDefault(Expression<Func<T, bool>> predicate)`: Get first matching entity
- `Select<TResult>(Expression<Func<T, TResult>> selector)`: Project entities
- `Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)`: Custom query

### Update Operations
- `Update(T entity)`: Update single entity
- `UpdateRange(IEnumerable<T> entities)`: Update multiple entities

### Delete Operations
- `Delete(T entity)`: Delete single entity
- `DeleteRange(IEnumerable<T> entities)`: Delete multiple entities
- `DeleteMany(Expression<Func<T, bool>> predicate)`: Delete by predicate

### Utility Operations
- `Count()`: Count all entities
- `Count(Expression<Func<T, bool>> predicate)`: Count by predicate
- `Exists(Expression<Func<T, bool>> predicate)`: Check existence

## Testing

The library includes comprehensive unit tests in the `tests/Codezerg.Data.Repository.Tests` project covering:
- Repository implementations
- Entity operations
- Thread safety
- Primary key detection
- Identity management

## Examples

Example implementations are available in `src/Codezerg.Data.Repository.Example/Examples/`:
- InMemoryExample.cs: Demonstrates in-memory repository usage
- DatabaseExample.cs: Shows SQLite database repository
- CachedExample.cs: Illustrates cached repository with persistence
- DependencyInjectionExample.cs: Dependency injection setup (Note: DI support code may need to be implemented)