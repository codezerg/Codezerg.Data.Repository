using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Core.Overrides;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests;

public class RepositoryOverrideManagerTests
{
    private readonly RepositoryOverrideManager _manager;
    
    public RepositoryOverrideManagerTests()
    {
        _manager = new RepositoryOverrideManager();
    }
    
    [Fact]
    public void Override_WithValidConfiguration_ShouldStoreOverride()
    {
        // Arrange & Act
        _manager.Override<SimpleEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.EnableLogging = true;
            config.BulkBatchSize = 2000;
        });
        
        // Assert
        _manager.HasOverride(typeof(SimpleEntity)).Should().BeTrue();
        
        var override1 = _manager.GetOverride(typeof(SimpleEntity));
        override1.Should().NotBeNull();
        override1!.Strategy.Should().Be(RepositoryStrategy.InMemory);
        override1.EnableLogging.Should().BeTrue();
        override1.BulkBatchSize.Should().Be(2000);
    }
    
    [Fact]
    public void OverrideFor_WithTypeParameter_ShouldStoreOverride()
    {
        // Arrange
        var entityType = typeof(DatabaseEntity);
        
        // Act
        _manager.OverrideFor(entityType, config =>
        {
            config.Strategy = RepositoryStrategy.Cached;
            config.CacheExpirationMinutes = 45;
            config.ConnectionString = "Data Source=test.db";
        });
        
        // Assert
        _manager.HasOverride(entityType).Should().BeTrue();
        
        var override1 = _manager.GetOverride(entityType);
        override1.Should().NotBeNull();
        override1!.Strategy.Should().Be(RepositoryStrategy.Cached);
        override1.CacheExpirationMinutes.Should().Be(45);
        override1.ConnectionString.Should().Be("Data Source=test.db");
    }
    
    [Fact]
    public void Override_CalledMultipleTimes_ShouldReplaceExistingOverride()
    {
        // Arrange
        _manager.Override<InMemoryEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.EnableLogging = false;
        });
        
        // Act - Override again with different settings
        _manager.Override<InMemoryEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Cached;
            config.EnableLogging = true;
            config.CacheExpirationMinutes = 60;
        });
        
        // Assert
        var override1 = _manager.GetOverride(typeof(InMemoryEntity));
        override1.Should().NotBeNull();
        override1!.Strategy.Should().Be(RepositoryStrategy.Cached);
        override1.EnableLogging.Should().BeTrue();
        override1.CacheExpirationMinutes.Should().Be(60);
    }
    
    [Fact]
    public void GetOverride_WhenNoOverrideExists_ShouldReturnNull()
    {
        // Arrange & Act
        var result = _manager.GetOverride(typeof(DefaultEntity));
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetOverride_ShouldReturnClonedConfiguration()
    {
        // Arrange
        _manager.Override<ComplexEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.BulkBatchSize = 3000;
        });
        
        // Act
        var override1 = _manager.GetOverride(typeof(ComplexEntity));
        var override2 = _manager.GetOverride(typeof(ComplexEntity));
        
        // Assert
        override1.Should().NotBeNull();
        override2.Should().NotBeNull();
        override1.Should().NotBeSameAs(override2); // Should be different instances
        
        // But with same values
        override1!.Strategy.Should().Be(override2!.Strategy);
        override1.BulkBatchSize.Should().Be(override2.BulkBatchSize);
        
        // Modifying one should not affect the other
        override1.BulkBatchSize = 5000;
        override2.BulkBatchSize.Should().Be(3000);
    }
    
    [Fact]
    public void HasOverride_WhenOverrideExists_ShouldReturnTrue()
    {
        // Arrange
        _manager.Override<ValidationEntity>(config => config.EnableLogging = true);
        
        // Act & Assert
        _manager.HasOverride(typeof(ValidationEntity)).Should().BeTrue();
    }
    
    [Fact]
    public void HasOverride_WhenNoOverrideExists_ShouldReturnFalse()
    {
        // Act & Assert
        _manager.HasOverride(typeof(BulkEntity)).Should().BeFalse();
    }
    
    [Fact]
    public void ClearOverride_WithGenericType_ShouldRemoveOverride()
    {
        // Arrange
        _manager.Override<ThreadSafeEntity>(config => config.Strategy = RepositoryStrategy.InMemory);
        _manager.HasOverride(typeof(ThreadSafeEntity)).Should().BeTrue();
        
        // Act
        _manager.ClearOverride<ThreadSafeEntity>();
        
        // Assert
        _manager.HasOverride(typeof(ThreadSafeEntity)).Should().BeFalse();
        _manager.GetOverride(typeof(ThreadSafeEntity)).Should().BeNull();
    }
    
    [Fact]
    public void ClearOverride_WithTypeParameter_ShouldRemoveOverride()
    {
        // Arrange
        var entityType = typeof(AuditedEntity);
        _manager.OverrideFor(entityType, config => config.LogQueries = true);
        _manager.HasOverride(entityType).Should().BeTrue();
        
        // Act
        _manager.ClearOverride(entityType);
        
        // Assert
        _manager.HasOverride(entityType).Should().BeFalse();
        _manager.GetOverride(entityType).Should().BeNull();
    }
    
    [Fact]
    public void ClearAllOverrides_ShouldRemoveAllOverrides()
    {
        // Arrange
        _manager.Override<SimpleEntity>(config => config.Strategy = RepositoryStrategy.InMemory);
        _manager.Override<DatabaseEntity>(config => config.EnableLogging = true);
        _manager.Override<CachedEntity>(config => config.CacheExpirationMinutes = 30);
        
        // Verify overrides exist
        _manager.HasOverride(typeof(SimpleEntity)).Should().BeTrue();
        _manager.HasOverride(typeof(DatabaseEntity)).Should().BeTrue();
        _manager.HasOverride(typeof(CachedEntity)).Should().BeTrue();
        
        // Act
        _manager.ClearAllOverrides();
        
        // Assert
        _manager.HasOverride(typeof(SimpleEntity)).Should().BeFalse();
        _manager.HasOverride(typeof(DatabaseEntity)).Should().BeFalse();
        _manager.HasOverride(typeof(CachedEntity)).Should().BeFalse();
    }
    
    [Fact]
    public void OverrideFor_WithNullEntityType_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => _manager.OverrideFor(null!, config => { });
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("entityType");
    }
    
    [Fact]
    public void OverrideFor_WithNullConfigureAction_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => _manager.OverrideFor(typeof(SimpleEntity), null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
    
    [Fact]
    public void Override_PreservesAttributeBasedConfiguration()
    {
        // Arrange
        // First clear any cached configurations
        AttributeConfigurationBuilder.ClearCache();
        
        // Act
        _manager.Override<CachedEntity>(config =>
        {
            // Only override specific properties
            config.EnableLogging = true;
            config.BulkBatchSize = 7500;
        });
        
        // Assert
        var override1 = _manager.GetOverride(typeof(CachedEntity));
        override1.Should().NotBeNull();
        
        // Overridden values
        override1!.EnableLogging.Should().BeTrue();
        override1.BulkBatchSize.Should().Be(7500);
        
        // Values from attributes should be preserved
        override1.Strategy.Should().Be(RepositoryStrategy.Cached);
        override1.CacheExpirationMinutes.Should().Be(30);
        override1.ConnectionPoolSize.Should().Be(20);
    }
    
    [Fact]
    public async Task Override_IsThreadSafe_ConcurrentOperations()
    {
        // Arrange
        var tasks = new Task[100];
        var random = new Random();
        var errors = new ConcurrentBag<Exception>();
        
        // Act - Perform concurrent operations
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    var operation = random.Next(5);
                    switch (operation)
                    {
                        case 0:
                            _manager.Override<SimpleEntity>(config =>
                            {
                                config.BulkBatchSize = index;
                                config.EnableLogging = index % 2 == 0;
                            });
                            break;
                        case 1:
                            _manager.GetOverride(typeof(SimpleEntity));
                            break;
                        case 2:
                            _manager.HasOverride(typeof(SimpleEntity));
                            break;
                        case 3:
                            _manager.ClearOverride<SimpleEntity>();
                            break;
                        case 4:
                            _manager.Override<DatabaseEntity>(config =>
                            {
                                config.ConnectionString = $"Data Source=test{index}.db";
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - No exceptions should have occurred
        errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Override_WithComplexConfiguration_ShouldStoreAllSettings()
    {
        // Arrange & Act
        _manager.Override<ComplexEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Cached;
            config.DatabaseName = "ComplexDB";
            config.TableName = "ComplexTable";
            config.ConnectionString = "Data Source=complex.db;Version=3;";
            config.ProviderName = "SQLite";
            config.BulkBatchSize = 10000;
            config.EnableLogging = true;
            config.PersistAcrossSessions = true;
            config.EnableWalMode = false;
            config.AutoCreateTable = false;
            config.CacheExpirationMinutes = 120;
            config.PreloadCache = true;
            config.ConnectionPoolSize = 50;
            config.CommandTimeout = 60;
            config.EnableQueryCache = true;
            config.QueryCacheDurationSeconds = 300;
            config.LogQueries = true;
            config.LogPerformanceMetrics = true;
            config.TrackChanges = true;
            config.AuditTableSuffix = "_Audit";
        });
        
        // Assert
        var override1 = _manager.GetOverride(typeof(ComplexEntity));
        override1.Should().NotBeNull();
        
        override1!.Strategy.Should().Be(RepositoryStrategy.Cached);
        override1.DatabaseName.Should().Be("ComplexDB");
        override1.TableName.Should().Be("ComplexTable");
        override1.ConnectionString.Should().Be("Data Source=complex.db;Version=3;");
        override1.ProviderName.Should().Be("SQLite");
        override1.BulkBatchSize.Should().Be(10000);
        override1.EnableLogging.Should().BeTrue();
        override1.PersistAcrossSessions.Should().BeTrue();
        override1.EnableWalMode.Should().BeFalse();
        override1.AutoCreateTable.Should().BeFalse();
        override1.CacheExpirationMinutes.Should().Be(120);
        override1.PreloadCache.Should().BeTrue();
        override1.ConnectionPoolSize.Should().Be(50);
        override1.CommandTimeout.Should().Be(60);
        override1.EnableQueryCache.Should().BeTrue();
        override1.QueryCacheDurationSeconds.Should().Be(300);
        override1.LogQueries.Should().BeTrue();
        override1.LogPerformanceMetrics.Should().BeTrue();
        override1.TrackChanges.Should().BeTrue();
        override1.AuditTableSuffix.Should().Be("_Audit");
    }
    
    [Fact]
    public void ClearOverride_OnNonExistentOverride_ShouldNotThrow()
    {
        // Arrange & Act
        var action = () => _manager.ClearOverride<DefaultEntity>();
        
        // Assert
        action.Should().NotThrow();
        _manager.HasOverride(typeof(DefaultEntity)).Should().BeFalse();
    }
    
    [Fact]
    public void Override_WithPartialConfiguration_ShouldMergeWithExisting()
    {
        // Arrange
        _manager.Override<ValidationEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.EnableLogging = true;
            config.ConnectionString = "Initial";
        });
        
        // Act - Override with partial configuration
        _manager.Override<ValidationEntity>(config =>
        {
            config.ConnectionString = "Updated";
            config.BulkBatchSize = 5000;
            // Note: Not setting Strategy or EnableLogging
        });
        
        // Assert
        var override1 = _manager.GetOverride(typeof(ValidationEntity));
        override1.Should().NotBeNull();
        
        // Updated values
        override1!.ConnectionString.Should().Be("Updated");
        override1.BulkBatchSize.Should().Be(5000);
        
        // Previously set values should be included
        override1.Strategy.Should().Be(RepositoryStrategy.Database);
        override1.EnableLogging.Should().BeTrue();
    }
}