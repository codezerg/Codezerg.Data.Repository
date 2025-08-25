using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Core.Overrides;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests;

public class ScopedRepositoryContextTests : IDisposable
{
    private readonly RepositoryOverrideManager _overrideManager;
    private readonly ScopedRepositoryContext _scopedContext;
    
    public ScopedRepositoryContextTests()
    {
        _overrideManager = new RepositoryOverrideManager();
        _scopedContext = new ScopedRepositoryContext(_overrideManager);
    }
    
    public void Dispose()
    {
        _scopedContext?.Dispose();
    }
    
    [Fact]
    public void Constructor_WithNullOverrideManager_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new ScopedRepositoryContext(null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("overrideManager");
    }
    
    [Fact]
    public void Override_ShouldApplyOverrideToManager()
    {
        // Arrange & Act
        _scopedContext.Override<SimpleEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.EnableLogging = true;
        });
        
        // Assert
        _overrideManager.HasOverride(typeof(SimpleEntity)).Should().BeTrue();
        var override1 = _overrideManager.GetOverride(typeof(SimpleEntity));
        override1.Should().NotBeNull();
        override1!.Strategy.Should().Be(RepositoryStrategy.InMemory);
        override1.EnableLogging.Should().BeTrue();
    }
    
    [Fact]
    public void OverrideFor_WithType_ShouldApplyOverrideToManager()
    {
        // Arrange
        var entityType = typeof(DatabaseEntity);
        
        // Act
        _scopedContext.OverrideFor(entityType, config =>
        {
            config.Strategy = RepositoryStrategy.Cached;
            config.CacheExpirationMinutes = 30;
        });
        
        // Assert
        _overrideManager.HasOverride(entityType).Should().BeTrue();
        var override1 = _overrideManager.GetOverride(entityType);
        override1.Should().NotBeNull();
        override1!.Strategy.Should().Be(RepositoryStrategy.Cached);
        override1.CacheExpirationMinutes.Should().Be(30);
    }
    
    [Fact]
    public void Dispose_ShouldRestoreOriginalConfiguration_WhenNoOriginalOverrideExisted()
    {
        // Arrange
        _overrideManager.HasOverride(typeof(InMemoryEntity)).Should().BeFalse();
        
        // Act
        _scopedContext.Override<InMemoryEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.EnableLogging = true;
        });
        
        // Verify override is applied
        _overrideManager.HasOverride(typeof(InMemoryEntity)).Should().BeTrue();
        
        // Dispose the context
        _scopedContext.Dispose();
        
        // Assert - override should be removed
        _overrideManager.HasOverride(typeof(InMemoryEntity)).Should().BeFalse();
        _overrideManager.GetOverride(typeof(InMemoryEntity)).Should().BeNull();
    }
    
    [Fact]
    public void Dispose_ShouldRestoreOriginalConfiguration_WhenOriginalOverrideExisted()
    {
        // Arrange - Set up original override
        _overrideManager.Override<CachedEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.ConnectionString = "Original";
            config.BulkBatchSize = 1000;
        });
        
        var originalOverride = _overrideManager.GetOverride(typeof(CachedEntity));
        originalOverride.Should().NotBeNull();
        
        // Act - Apply scoped override
        _scopedContext.Override<CachedEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.ConnectionString = "Scoped";
            config.EnableLogging = true;
        });
        
        // Verify scoped override is applied
        var scopedOverride = _overrideManager.GetOverride(typeof(CachedEntity));
        scopedOverride!.Strategy.Should().Be(RepositoryStrategy.InMemory);
        scopedOverride.ConnectionString.Should().Be("Scoped");
        scopedOverride.EnableLogging.Should().BeTrue();
        
        // Dispose the context
        _scopedContext.Dispose();
        
        // Assert - original override should be restored
        var restoredOverride = _overrideManager.GetOverride(typeof(CachedEntity));
        restoredOverride.Should().NotBeNull();
        restoredOverride!.Strategy.Should().Be(RepositoryStrategy.Database);
        restoredOverride.ConnectionString.Should().Be("Original");
        restoredOverride.BulkBatchSize.Should().Be(1000);
        restoredOverride.EnableLogging.Should().BeFalse(); // Should not have scoped value
    }
    
    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldOnlyRestoreOnce()
    {
        // Arrange
        _scopedContext.Override<ComplexEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
        });
        
        // Act
        _scopedContext.Dispose();
        _overrideManager.HasOverride(typeof(ComplexEntity)).Should().BeFalse();
        
        // Override again after disposal
        _overrideManager.Override<ComplexEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
        });
        
        // Dispose again
        _scopedContext.Dispose();
        
        // Assert - the new override should still exist
        _overrideManager.HasOverride(typeof(ComplexEntity)).Should().BeTrue();
        var override1 = _overrideManager.GetOverride(typeof(ComplexEntity));
        override1!.Strategy.Should().Be(RepositoryStrategy.Database);
    }
    
    [Fact]
    public void Override_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _scopedContext.Dispose();
        
        // Act & Assert
        var action = () => _scopedContext.Override<SimpleEntity>(config => { });
        action.Should().Throw<ObjectDisposedException>();
    }
    
    [Fact]
    public void OverrideFor_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _scopedContext.Dispose();
        
        // Act & Assert
        var action = () => _scopedContext.OverrideFor(typeof(SimpleEntity), config => { });
        action.Should().Throw<ObjectDisposedException>();
    }
    
    [Fact]
    public void ConfigureOverrides_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _scopedContext.Dispose();
        
        // Act & Assert
        var action = () => _scopedContext.ConfigureOverrides();
        action.Should().Throw<ObjectDisposedException>();
    }
    
    [Fact]
    public void Override_MultipleEntities_ShouldRestoreAllOnDispose()
    {
        // Arrange
        _overrideManager.Override<SimpleEntity>(config => config.Strategy = RepositoryStrategy.Database);
        _overrideManager.Override<DatabaseEntity>(config => config.EnableLogging = true);
        // ThreadSafeEntity has no original override
        
        // Act - Apply scoped overrides
        _scopedContext
            .Override<SimpleEntity>(config => config.Strategy = RepositoryStrategy.InMemory)
            .Override<DatabaseEntity>(config => config.EnableLogging = false)
            .Override<ThreadSafeEntity>(config => config.Strategy = RepositoryStrategy.Cached);
        
        // Verify scoped overrides are applied
        _overrideManager.GetOverride(typeof(SimpleEntity))!.Strategy.Should().Be(RepositoryStrategy.InMemory);
        _overrideManager.GetOverride(typeof(DatabaseEntity))!.EnableLogging.Should().BeFalse();
        _overrideManager.GetOverride(typeof(ThreadSafeEntity))!.Strategy.Should().Be(RepositoryStrategy.Cached);
        
        // Dispose
        _scopedContext.Dispose();
        
        // Assert - verify restoration
        _overrideManager.GetOverride(typeof(SimpleEntity))!.Strategy.Should().Be(RepositoryStrategy.Database);
        _overrideManager.GetOverride(typeof(DatabaseEntity))!.EnableLogging.Should().BeTrue();
        _overrideManager.HasOverride(typeof(ThreadSafeEntity)).Should().BeFalse();
    }
    
    [Fact]
    public void ConfigureOverrides_ShouldReturnFluentBuilder()
    {
        // Arrange & Act
        var builder = _scopedContext.ConfigureOverrides();
        
        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<ScopedFluentOverrideBuilder>();
    }
    
    [Fact]
    public void ConfigureOverrides_WithFluentBuilder_ShouldApplyOverrides()
    {
        // Arrange & Act
        _scopedContext.ConfigureOverrides()
            .ForEntity<ValidationEntity>()
                .UseInMemory(options => options.PersistAcrossSessions = true)
                .And()
            .ForEntity<AuditedEntity>()
                .UseDatabase(options => options.ConnectionString = "Data Source=audit.db")
                .Apply();
        
        // Assert
        
        // Verify overrides were applied
        var validationOverride = _overrideManager.GetOverride(typeof(ValidationEntity));
        validationOverride.Should().NotBeNull();
        validationOverride!.Strategy.Should().Be(RepositoryStrategy.InMemory);
        validationOverride.PersistAcrossSessions.Should().BeTrue();
        
        var auditOverride = _overrideManager.GetOverride(typeof(AuditedEntity));
        auditOverride.Should().NotBeNull();
        auditOverride!.Strategy.Should().Be(RepositoryStrategy.Database);
        auditOverride.ConnectionString.Should().Be("Data Source=audit.db");
    }
    
    [Fact]
    public void Override_ChainedCalls_ShouldReturnSelfForFluency()
    {
        // Arrange & Act
        var result = _scopedContext
            .Override<SimpleEntity>(config => config.Strategy = RepositoryStrategy.InMemory)
            .Override<DatabaseEntity>(config => config.EnableLogging = true)
            .OverrideFor(typeof(CachedEntity), config => config.CacheExpirationMinutes = 60);
        
        // Assert
        result.Should().BeSameAs(_scopedContext);
        
        // Verify all overrides were applied
        _overrideManager.HasOverride(typeof(SimpleEntity)).Should().BeTrue();
        _overrideManager.HasOverride(typeof(DatabaseEntity)).Should().BeTrue();
        _overrideManager.HasOverride(typeof(CachedEntity)).Should().BeTrue();
    }
    
    [Fact]
    public void NestedScopes_ShouldRestoreInCorrectOrder()
    {
        // Arrange - Original override
        _overrideManager.Override<ComplexEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.ConnectionString = "Original";
        });
        
        // Create first scope
        using (var scope1 = new ScopedRepositoryContext(_overrideManager))
        {
            scope1.Override<ComplexEntity>(config =>
            {
                config.Strategy = RepositoryStrategy.Cached;
                config.ConnectionString = "Scope1";
            });
            
            // Verify scope1 override
            _overrideManager.GetOverride(typeof(ComplexEntity))!.ConnectionString.Should().Be("Scope1");
            
            // Create nested scope
            using (var scope2 = new ScopedRepositoryContext(_overrideManager))
            {
                scope2.Override<ComplexEntity>(config =>
                {
                    config.Strategy = RepositoryStrategy.InMemory;
                    config.ConnectionString = "Scope2";
                });
                
                // Verify scope2 override
                _overrideManager.GetOverride(typeof(ComplexEntity))!.ConnectionString.Should().Be("Scope2");
            }
            
            // After scope2 disposal, should restore to scope1
            _overrideManager.GetOverride(typeof(ComplexEntity))!.ConnectionString.Should().Be("Scope1");
        }
        
        // After scope1 disposal, should restore to original
        _overrideManager.GetOverride(typeof(ComplexEntity))!.ConnectionString.Should().Be("Original");
    }
    
    [Fact]
    public void ComplexConfiguration_ShouldBeFullyRestored()
    {
        // Arrange - Set complex original configuration
        _overrideManager.Override<BulkEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            config.DatabaseName = "BulkDB";
            config.TableName = "BulkTable";
            config.ConnectionString = "Data Source=bulk.db";
            config.ProviderName = "SQLite";
            config.BulkBatchSize = 5000;
            config.EnableLogging = true;
            config.EnableWalMode = true;
            config.AutoCreateTable = true;
            config.ConnectionPoolSize = 20;
            config.CommandTimeout = 30;
            config.EnableQueryCache = true;
            config.QueryCacheDurationSeconds = 120;
            config.LogQueries = true;
            config.LogPerformanceMetrics = true;
            config.TrackChanges = true;
            config.AuditTableSuffix = "_Audit";
        });
        
        var originalConfig = _overrideManager.GetOverride(typeof(BulkEntity));
        
        // Act - Apply scoped override with different values
        _scopedContext.Override<BulkEntity>(config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            config.DatabaseName = "ScopedDB";
            config.TableName = "ScopedTable";
            config.ConnectionString = "Data Source=scoped.db";
            config.ProviderName = "InMemory";
            config.BulkBatchSize = 10000;
            config.EnableLogging = false;
            config.PersistAcrossSessions = true;
            config.EnableWalMode = false;
            config.AutoCreateTable = false;
            config.ConnectionPoolSize = 50;
            config.CommandTimeout = 60;
            config.EnableQueryCache = false;
            config.QueryCacheDurationSeconds = 60;
            config.LogQueries = false;
            config.LogPerformanceMetrics = false;
            config.TrackChanges = false;
            config.AuditTableSuffix = "_History";
        });
        
        // Dispose
        _scopedContext.Dispose();
        
        // Assert - All original values should be restored
        var restoredConfig = _overrideManager.GetOverride(typeof(BulkEntity));
        restoredConfig.Should().NotBeNull();
        
        restoredConfig!.Strategy.Should().Be(originalConfig!.Strategy);
        restoredConfig.DatabaseName.Should().Be(originalConfig.DatabaseName);
        restoredConfig.TableName.Should().Be(originalConfig.TableName);
        restoredConfig.ConnectionString.Should().Be(originalConfig.ConnectionString);
        restoredConfig.ProviderName.Should().Be(originalConfig.ProviderName);
        restoredConfig.BulkBatchSize.Should().Be(originalConfig.BulkBatchSize);
        restoredConfig.EnableLogging.Should().Be(originalConfig.EnableLogging);
        restoredConfig.EnableWalMode.Should().Be(originalConfig.EnableWalMode);
        restoredConfig.AutoCreateTable.Should().Be(originalConfig.AutoCreateTable);
        restoredConfig.ConnectionPoolSize.Should().Be(originalConfig.ConnectionPoolSize);
        restoredConfig.CommandTimeout.Should().Be(originalConfig.CommandTimeout);
        restoredConfig.EnableQueryCache.Should().Be(originalConfig.EnableQueryCache);
        restoredConfig.QueryCacheDurationSeconds.Should().Be(originalConfig.QueryCacheDurationSeconds);
        restoredConfig.LogQueries.Should().Be(originalConfig.LogQueries);
        restoredConfig.LogPerformanceMetrics.Should().Be(originalConfig.LogPerformanceMetrics);
        restoredConfig.TrackChanges.Should().Be(originalConfig.TrackChanges);
        restoredConfig.AuditTableSuffix.Should().Be(originalConfig.AuditTableSuffix);
        
        // Scoped-only value should not be present
        restoredConfig.PersistAcrossSessions.Should().BeFalse();
    }
}