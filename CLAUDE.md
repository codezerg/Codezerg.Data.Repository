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
```

## Architecture Overview

This is a .NET 8 repository pattern library providing three implementation strategies with a unified `IRepository<T>` interface:

1. **InMemoryRepository<T>**: Pure in-memory storage with deep copy protection to prevent external modifications
2. **DatabaseRepository<T>**: Direct database access using linq2db with SQLite support, includes automatic table creation
3. **CachedRepository<T>**: Hybrid approach combining in-memory caching with SQLite persistence, using ReaderWriterLockSlim for thread safety

### Key Architectural Components

- **IRepository<T>**: Core interface defining CRUD operations (src/Codezerg.Data.Repository/IRepository.cs:8)
- **EntityOperations<T>**: Handles entity manipulation, identity management, and deep copying
- **EntityMapping<T>**: Manages database mapping schemas and table names
- **IdentityManager**: Manages auto-incrementing identity values for entities
- **RepositoryFactory**: Creates CachedRepository instances with SQLite backing stored in Data/ folder
- **RepositoryProxy<T>**: Service container proxy for lazy repository instantiation

### Repository Selection Strategy

- Default factory creates `CachedRepository<T>` with SQLite backing
- SQLite databases are stored in `{AppDomain.CurrentDomain.BaseDirectory}/Data/{DatabaseName}.db`
- Database names are derived from entity type via `EntityMapping<T>.GetDatabaseName()`

### Thread Safety

- InMemoryRepository: Not thread-safe, uses deep copying for data isolation
- DatabaseRepository: Thread-safe through connection-per-operation pattern
- CachedRepository: Fully thread-safe using ReaderWriterLockSlim

### Dependency Injection

Use `services.AddRepositoryServices()` to register:
- `IRepositoryFactory` as singleton
- `IRepository<T>` as singleton (via RepositoryProxy<T>)

## Package Dependencies

- linq2db (5.4.1): ORM for database operations
- Microsoft.Data.Sqlite (9.0.1): SQLite provider
- Microsoft.Extensions.DependencyInjection.Abstractions (8.0.0): DI abstractions

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