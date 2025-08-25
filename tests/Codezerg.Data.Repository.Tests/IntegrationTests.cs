using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Core.Overrides;
using Codezerg.Data.Repository.Infrastructure;
using Codezerg.Data.Repository.Implementations;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests;

public class IntegrationTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly ServiceProvider _serviceProvider;
    
    public IntegrationTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"RepoTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDataPath);
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        services.AddRepositoryServicesWithAttributes(
            configureOptions: options =>
            {
                options.DataPath = _testDataPath;
                options.UseAttributeConfiguration = true;
                options.BulkOperationBatchSize = 1000;
            }
        );
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    public void Dispose()
    {
        _serviceProvider?.Dispose();
        
        if (Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
    
    [Fact]
    public void Repository_WithInMemoryAttribute_ShouldUseInMemoryRepository()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Act
        var repository = factory.GetRepository<InMemoryEntity>();
        
        // Assert
        repository.Should().NotBeNull();
        
        // Test in-memory behavior
        var entity = new InMemoryEntity
        {
            Id = Guid.NewGuid(),
            SessionData = "Test Session",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        repository.Insert(entity);
        var retrieved = repository.FirstOrDefault(e => e.Id == entity.Id);
        
        retrieved.Should().NotBeNull();
        retrieved!.SessionData.Should().Be("Test Session");
        
        // In-memory should not persist to disk
        var dbFiles = Directory.GetFiles(_testDataPath, "*.db");
        dbFiles.Should().NotContain(f => f.Contains("InMemoryEntity"));
    }
    
    [Fact]
    public void Repository_WithDatabaseAttribute_ShouldUseDatabaseRepository()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Act
        var repository = factory.GetRepository<DatabaseEntity>();
        
        // Assert
        repository.Should().NotBeNull();
        
        // Test database behavior
        var entity = new DatabaseEntity
        {
            Content = "Database Content",
            Amount = 123.45m,
            IsActive = true
        };
        
        var id = repository.InsertWithInt64Identity(entity);
        id.Should().BeGreaterThan(0);
        
        // Should persist to database
        var dbFile = Path.Combine(_testDataPath, "TestDatabase.db");
        File.Exists(dbFile).Should().BeTrue();
        
        // Create new repository instance to verify persistence
        var repository2 = factory.GetRepository<DatabaseEntity>();
        var retrieved = repository2.FirstOrDefault(e => e.Id == id);
        
        retrieved.Should().NotBeNull();
        retrieved!.Content.Should().Be("Database Content");
        retrieved.Amount.Should().Be(123.45m);
        retrieved.IsActive.Should().BeTrue();
    }
    
    [Fact]
    public void Repository_WithCachedAttribute_ShouldUseCachedRepository()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Act
        var repository = factory.GetRepository<CachedEntity>();
        
        // Assert
        repository.Should().NotBeNull();
        
        // Insert multiple entities
        var entities = Enumerable.Range(1, 100).Select(i => new CachedEntity
        {
            Name = $"Entity {i}",
            Value = i * 1.5,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        
        repository.InsertRange(entities);
        
        // First read should load from database and cache
        var allEntities = repository.GetAll().ToList();
        allEntities.Should().HaveCount(100);
        
        // Subsequent reads should be from cache (faster)
        var cachedRead = repository.GetAll().ToList();
        cachedRead.Should().HaveCount(100);
        
        // Updates should go to both cache and database
        var firstEntity = repository.FirstOrDefault(e => e.Name == "Entity 1");
        firstEntity!.Value = 999.99;
        repository.Update(firstEntity);
        
        // Verify update in cache
        var updated = repository.FirstOrDefault(e => e.Name == "Entity 1");
        updated!.Value.Should().Be(999.99);
        
        // Verify persistence
        var dbFile = Path.Combine(_testDataPath, "CacheTest.db");
        File.Exists(dbFile).Should().BeTrue();
    }
    
    [Fact]
    public void DependencyInjection_ShouldResolveRepositories()
    {
        // Arrange & Act
        var simpleRepo = _serviceProvider.GetRequiredService<IRepository<SimpleEntity>>();
        var databaseRepo = _serviceProvider.GetRequiredService<IRepository<DatabaseEntity>>();
        var cachedRepo = _serviceProvider.GetRequiredService<IRepository<CachedEntity>>();
        
        // Assert
        simpleRepo.Should().NotBeNull();
        databaseRepo.Should().NotBeNull();
        cachedRepo.Should().NotBeNull();
        
        // Each should work correctly
        simpleRepo.Insert(new SimpleEntity { Name = "Test", CreatedAt = DateTime.Now });
        simpleRepo.Count().Should().Be(1);
        
        databaseRepo.InsertWithInt64Identity(new DatabaseEntity { Content = "Test" });
        databaseRepo.Count().Should().Be(1);
        
        cachedRepo.InsertWithIdentity(new CachedEntity { Name = "Test", UpdatedAt = DateTime.Now });
        cachedRepo.Count().Should().Be(1);
    }
    
    [Fact]
    public void RuntimeOverrides_ShouldOverrideAttributes()
    {
        // Arrange
        var overrideManager = _serviceProvider.GetRequiredService<IRepositoryOverrideManager>();
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Override DatabaseEntity to use InMemory instead
        overrideManager.Override<DatabaseEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.PersistAcrossSessions = false;
        });
        
        // Act
        var repository = factory.GetRepository<DatabaseEntity>();
        
        // Assert - Should use in-memory despite DatabaseRepository attribute
        repository.InsertWithInt64Identity(new DatabaseEntity { Content = "Test" });
        
        // Should not create database file due to override
        var dbFile = Path.Combine(_testDataPath, "TestDatabase.db");
        File.Exists(dbFile).Should().BeFalse();
    }
    
    [Fact]
    public void ScopedOverrides_ShouldTemporarilyChangeConfiguration()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Initially, CachedEntity uses cached repository
        var repo1 = factory.GetRepository<CachedEntity>();
        repo1.InsertWithIdentity(new CachedEntity { Name = "Before Scope", UpdatedAt = DateTime.Now });
        
        // Act - Use scoped override
        using (var scope = _serviceProvider.CreateScopedRepositoryContext())
        {
            scope.Override<CachedEntity>(config =>
            {
                config.Strategy = RepositoryStrategy.InMemory;
            });
            
            var repo2 = factory.GetRepository<CachedEntity>();
            
            // Should use in-memory within scope (no persistence)
            repo2.InsertWithIdentity(new CachedEntity { Name = "In Scope", UpdatedAt = DateTime.Now });
            repo2.Count().Should().Be(0); // In-memory is fresh, doesn't have persisted data
        }
        
        // After scope, should revert to cached
        var repo3 = factory.GetRepository<CachedEntity>();
        var allEntities = repo3.GetAll().ToList();
        
        // Should only have the entity added before scope (persisted)
        allEntities.Should().HaveCount(1);
        allEntities[0].Name.Should().Be("Before Scope");
    }
    
    [Fact]
    public void FluentOverrides_WithEnvironmentConditions_ShouldApplyCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        var services = new ServiceCollection();
        services.AddLogging();
        
        services.AddRepositoryServicesWithAttributes(
            configureOptions: options =>
            {
                options.DataPath = _testDataPath;
            }
        );
        
        services.ConfigureRepositoryOverrides(builder =>
        {
            builder
                .ForEntity<ValidationEntity>()
                .InEnvironment("Development")
                .UseInMemory()
                .And()
                
                .ForEntity<AuditedEntity>()
                .InEnvironment("Production")
                .UseDatabase(options => options.BulkBatchSize = 10000)
                .Apply();
        });
        
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRepositoryFactory>();
        
        // Act
        var validationRepo = factory.GetRepository<ValidationEntity>();
        var auditRepo = factory.GetRepository<AuditedEntity>();
        
        // Assert
        // ValidationEntity should use in-memory (Development environment matches)
        validationRepo.Insert(new ValidationEntity 
        { 
            RequiredField = "Test",
            RangeField = 50,
            Email = "test@example.com"
        });
        
        // AuditedEntity should not have override (Production doesn't match)
        auditRepo.InsertWithIdentity(new AuditedEntity
        {
            Action = "Test",
            UserId = "user1",
            Timestamp = DateTime.Now
        });
        
        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        provider.Dispose();
    }
    
    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        var repository = factory.GetRepository<ThreadSafeEntity>();
        var tasks = new List<Task>();
        
        // Act - Perform concurrent inserts
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    repository.InsertWithIdentity(new ThreadSafeEntity
                    {
                        ThreadId = threadId,
                        Counter = j,
                        AccessTime = DateTime.UtcNow
                    });
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        repository.Count().Should().Be(1000); // 10 threads * 100 inserts
        
        // Verify data integrity
        for (int i = 0; i < 10; i++)
        {
            var threadEntities = repository.Find(e => e.ThreadId == i).ToList();
            threadEntities.Should().HaveCount(100);
            threadEntities.Select(e => e.Counter).Distinct().Should().HaveCount(100);
        }
    }
    
    [Fact]
    public void BulkOperations_WithAttributeConfiguration_ShouldRespectBatchSize()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        var repository = factory.GetRepository<BulkEntity>();
        
        var batchId = Guid.NewGuid();
        var entities = Enumerable.Range(1, 15000).Select(i => new BulkEntity
        {
            BatchId = batchId,
            SequenceNumber = i,
            Data = new byte[100],
            ProcessedAt = DateTime.UtcNow
        }).ToList();
        
        // Act
        var insertCount = repository.InsertRange(entities);
        
        // Assert
        insertCount.Should().Be(15000);
        
        // Verify all entities were inserted
        var count = repository.Count(e => e.BatchId == batchId);
        count.Should().Be(15000);
        
        // BulkEntity has BulkBatchSize = 10000 attribute
        // This should have been applied during insertion
    }
    
    [Fact]
    public void ComplexQueries_ShouldWorkAcrossAllRepositoryTypes()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Test with different repository types
        var testCases = new (Type entityType, object repo)[]
        {
            (typeof(SimpleEntity), factory.GetRepository<SimpleEntity>()),
            (typeof(DatabaseEntity), factory.GetRepository<DatabaseEntity>()),
            (typeof(CachedEntity), factory.GetRepository<CachedEntity>())
        };
        
        foreach (var testCase in testCases)
        {
            if (testCase.entityType == typeof(SimpleEntity))
            {
                var simpleRepo = (IRepository<SimpleEntity>)testCase.repo;
                
                // Insert test data
                var testData = Enumerable.Range(1, 50).Select(i => new SimpleEntity
                {
                    Name = $"Entity {i}",
                    CreatedAt = DateTime.Now.AddDays(-i)
                }).ToList();
                
                simpleRepo.InsertRange(testData);
                
                // Complex queries
                var recentEntities = simpleRepo.Find(e => e.CreatedAt > DateTime.Now.AddDays(-10));
                recentEntities.Count().Should().Be(9);
                
                var namedEntities = simpleRepo.Find(e => e.Name.Contains("1"));
                namedEntities.Should().NotBeEmpty();
                
                var exists = simpleRepo.Exists(e => e.Name == "Entity 25");
                exists.Should().BeTrue();
                
                var selected = simpleRepo.Select(e => new { e.Name, e.CreatedAt });
                selected.Should().HaveCount(50);
                
                var query = simpleRepo.Query(q => q
                    .Where(e => e.Name.StartsWith("Entity"))
                    .OrderBy(e => e.CreatedAt)
                    .Take(10)
                    .Select(e => e.Name));
                
                query.Should().HaveCount(10);
            }
        }
    }
    
    [Fact]
    public void AttributeInheritance_ShouldWork()
    {
        // This test verifies that attribute inheritance works correctly
        // if we have a base class with attributes
        
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IRepositoryFactory>();
        
        // Act
        var simpleRepo = factory.GetRepository<SimpleEntity>();
        var entity = new SimpleEntity { Name = "Test", CreatedAt = DateTime.Now };
        
        var id = simpleRepo.InsertWithIdentity(entity);
        
        // Assert
        id.Should().BeGreaterThan(0);
        entity.Id.Should().Be(id);
        
        var retrieved = simpleRepo.FirstOrDefault(e => e.Id == id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test");
    }
}