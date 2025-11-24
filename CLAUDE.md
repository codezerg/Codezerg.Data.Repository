# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
dotnet run --project demo/Codezerg.Data.Repository.Example
```

## Architecture Overview

This is a .NET Standard 2.0 repository pattern library providing three implementation strategies with a unified `IRepository<T>` interface:

1. **InMemoryRepository<T>**: Thread-safe in-memory storage with deep copy protection to prevent external modifications, uses ReaderWriterLockSlim for thread safety
2. **DatabaseRepository<T>**: Direct database access using linq2db with SQLite support, includes automatic table creation, thread-safe through connection-per-operation pattern
3. **CachedRepository<T>**: Hybrid approach combining in-memory caching with SQLite persistence, fully thread-safe using ReaderWriterLockSlim

### Key Architectural Components

- **IRepository<T>**: Core interface defining CRUD operations (src/Codezerg.Data.Repository/IRepository.cs:8)
- **EntityOperations<T>**: Handles entity manipulation, identity management, and deep copying (src/Codezerg.Data.Repository/EntityOperations.cs:13)
- **EntityMapping<T>**: Manages database mapping schemas and table names (src/Codezerg.Data.Repository/EntityMapping.cs:13)
- **PrimaryKeyHelper<T>**: Manages primary key detection and operations (src/Codezerg.Data.Repository/PrimaryKeyHelper.cs)
- **IdentityManager<T>**: Manages auto-incrementing identity values for entities (src/Codezerg.Data.Repository/IdentityManager.cs)
- **EntityCloner<T>**: Creates deep copies of entities for data isolation (src/Codezerg.Data.Repository/EntityCloner.cs)
- **EntityMerger<T>**: Updates entity properties while preserving primary keys (src/Codezerg.Data.Repository/EntityMerger.cs)

### Automatic Schema Migration Components

- **SchemaManager<T>**: Orchestrates automatic schema migrations, called during repository initialization (src/Codezerg.Data.Repository/Migration/SchemaManager.cs)
- **SchemaInspector**: Queries database schema to detect tables and columns (src/Codezerg.Data.Repository/Migration/SchemaInspector.cs)
- **SchemaMigrator**: Applies schema changes (CREATE TABLE, ADD COLUMN, ALTER COLUMN) (src/Codezerg.Data.Repository/Migration/SchemaMigrator.cs)
- **TableColumn**: Model representing database column metadata (src/Codezerg.Data.Repository/Migration/TableColumn.cs)

### Repository Selection Strategy

- All three repository types implement `IRepository<T>` interface
- InMemoryRepository: Best for unit testing and temporary data storage
- DatabaseRepository: Direct database operations with minimal overhead
- CachedRepository: Optimal for read-heavy scenarios with persistence requirements
- SQLite databases are stored relative to AppDomain.CurrentDomain.BaseDirectory
- Database names are derived from entity type via `EntityMapping<T>.GetDatabaseName()`

### Automatic Schema Migrations

The library includes automatic schema migration support for DatabaseRepository and CachedRepository:

- **Automatic Table Creation**: Tables are created automatically if they don't exist
- **Column Addition**: When properties are added to entities, corresponding columns are added to the database
- **Column Alteration**: Changes to column types or nullability are detected and applied
- **SQLite Special Handling**: Uses table recreation pattern for ALTER COLUMN operations (SQLite limitation)
- **Thread-Safe**: Schema migrations are protected with locks to prevent concurrent modifications
- **One-Time Execution**: Each migration is applied only once per application lifecycle
- **Zero Configuration**: No migration files or version tracking required

Migration workflow:
1. Repository constructor calls `SchemaManager<T>.EnsureSchema()`
2. `SchemaInspector` checks if table exists and queries current schema
3. Compares entity definition with database schema
4. `SchemaMigrator` applies necessary changes (CREATE TABLE, ADD COLUMN, ALTER COLUMN)

Limitations:
- Column renames are not detected (treated as drop + add)
- Column deletions are not supported (old columns remain)
- Index management not included
- Complex constraints not supported

### Thread Safety

- **InMemoryRepository**: Fully thread-safe using ReaderWriterLockSlim with NoRecursion policy
- **DatabaseRepository**: Thread-safe through connection-per-operation pattern
- **CachedRepository**: Fully thread-safe using ReaderWriterLockSlim for coordinating memory and database operations
- **Schema Migrations**: Protected with locks in SchemaManager to prevent concurrent schema changes

### Attribute Support

The library uses linq2db attributes for entity mapping:
- `[PrimaryKey]` - Marks primary key properties
- `[Identity]` - Marks auto-incrementing identity columns
- `[Column]` - Specifies column name, type, nullability, and length
- `[Table]` - Specifies table name
- `[NotColumn]` - Excludes property from mapping

### Package Dependencies

- **linq2db (5.4.1)**: ORM for database operations
- **Microsoft.Data.Sqlite (9.0.1)**: SQLite provider (auto-referenced via linq2db)
- **Target Framework**: .NET Standard 2.0 (C# 7.3)

## Test Framework

The test project uses:
- **xUnit** as the test framework
- **FluentAssertions** for readable test assertions
- **Moq** for mocking
- **Microsoft.Extensions.DependencyInjection** for DI testing

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

## Examples

Example implementations are available in `demo/Codezerg.Data.Repository.Example/Examples/`:
- InMemoryExample.cs: Demonstrates in-memory repository usage
- DatabaseExample.cs: Shows SQLite database repository
- CachedExample.cs: Illustrates cached repository with persistence
- DependencyInjectionExample.cs: Dependency injection setup