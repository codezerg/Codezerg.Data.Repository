using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests;

public class AttributeConfigurationBuilderTests
{
    [Fact]
    public void BuildConfiguration_WithNoAttributes_ShouldReturnDefaultConfiguration()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<DefaultEntity>();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(RepositoryStrategy.Cached); // Default strategy
        config.EnableLogging.Should().BeFalse();
        config.BulkBatchSize.Should().Be(1000); // Default batch size
        config.EnableWalMode.Should().BeTrue();
        config.AutoCreateTable.Should().BeTrue();
        config.PreloadCache.Should().BeTrue();
        config.ConnectionPoolSize.Should().Be(10);
        config.CommandTimeout.Should().Be(30);
    }
    
    [Fact]
    public void BuildConfiguration_WithInMemoryAttribute_ShouldConfigureInMemorySettings()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<InMemoryEntity>();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(RepositoryStrategy.InMemory);
        config.PersistAcrossSessions.Should().BeTrue();
        
        // Other settings should remain default
        config.EnableLogging.Should().BeFalse();
        config.BulkBatchSize.Should().Be(1000);
    }
    
    [Fact]
    public void BuildConfiguration_WithDatabaseAttribute_ShouldConfigureDatabaseSettings()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<DatabaseEntity>();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(RepositoryStrategy.Database);
        config.DatabaseName.Should().Be("TestDatabase");
        config.EnableWalMode.Should().BeTrue();
        config.BulkBatchSize.Should().Be(5000);
        config.AutoCreateTable.Should().BeTrue();
        
        // InMemory specific settings should not be affected
        config.PersistAcrossSessions.Should().BeFalse();
    }
    
    [Fact]
    public void BuildConfiguration_WithCachedAndPerformanceAttributes_ShouldConfigureBothSettings()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<CachedEntity>();
        
        // Assert
        config.Should().NotBeNull();
        
        // Cached settings
        config.Strategy.Should().Be(RepositoryStrategy.Cached);
        config.CacheExpirationMinutes.Should().Be(30);
        config.PreloadCache.Should().BeFalse();
        config.DatabaseName.Should().Be("CacheTest");
        
        // Performance settings
        config.ConnectionPoolSize.Should().Be(20);
        config.CommandTimeout.Should().Be(10);
        config.EnableQueryCache.Should().BeTrue();
        config.QueryCacheDurationSeconds.Should().Be(120);
    }
    
    [Fact]
    public void BuildConfiguration_WithAuditAttribute_ShouldConfigureAuditSettings()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<AuditedEntity>();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(RepositoryStrategy.Database);
        config.LogQueries.Should().BeTrue();
        config.LogPerformanceMetrics.Should().BeTrue();
        config.TrackChanges.Should().BeTrue();
        config.AuditTableSuffix.Should().Be("_History");
    }
    
    [Fact]
    public void BuildConfiguration_CalledMultipleTimes_ShouldUseCachedResult()
    {
        // Arrange
        AttributeConfigurationBuilder.ClearCache(); // Ensure clean state
        
        // Act
        var config1 = AttributeConfigurationBuilder.BuildConfiguration<CachedEntity>();
        var config2 = AttributeConfigurationBuilder.BuildConfiguration<CachedEntity>();
        var config3 = AttributeConfigurationBuilder.BuildConfiguration<CachedEntity>();
        
        // Assert
        config1.Should().NotBeNull();
        config2.Should().NotBeNull();
        config3.Should().NotBeNull();
        
        // Configurations should have same values but be different instances (due to Clone)
        config1.Should().NotBeSameAs(config2);
        config1.Should().NotBeSameAs(config3);
        config2.Should().NotBeSameAs(config3);
        
        config1.Strategy.Should().Be(config2.Strategy);
        config1.CacheExpirationMinutes.Should().Be(config2.CacheExpirationMinutes);
        config1.ConnectionPoolSize.Should().Be(config2.ConnectionPoolSize);
    }
    
    [Fact]
    public void ClearCache_ShouldRemoveAllCachedConfigurations()
    {
        // Arrange
        var config1 = AttributeConfigurationBuilder.BuildConfiguration<InMemoryEntity>();
        config1.Should().NotBeNull();
        
        // Act
        AttributeConfigurationBuilder.ClearCache();
        
        // After clearing cache, building configuration should work again
        var config2 = AttributeConfigurationBuilder.BuildConfiguration<InMemoryEntity>();
        
        // Assert
        config2.Should().NotBeNull();
        config2.Strategy.Should().Be(RepositoryStrategy.InMemory);
    }
    
    [Fact]
    public void ClearCache_WithSpecificType_ShouldOnlyRemoveThatTypeFromCache()
    {
        // Arrange
        AttributeConfigurationBuilder.ClearCache(); // Start clean
        var config1 = AttributeConfigurationBuilder.BuildConfiguration<InMemoryEntity>();
        var config2 = AttributeConfigurationBuilder.BuildConfiguration<DatabaseEntity>();
        
        // Act
        AttributeConfigurationBuilder.ClearCache(typeof(InMemoryEntity));
        
        // Building InMemoryEntity config should work (was cleared)
        var config3 = AttributeConfigurationBuilder.BuildConfiguration<InMemoryEntity>();
        // DatabaseEntity should still use cached version
        var config4 = AttributeConfigurationBuilder.BuildConfiguration<DatabaseEntity>();
        
        // Assert
        config3.Should().NotBeNull();
        config4.Should().NotBeNull();
        config3.Strategy.Should().Be(RepositoryStrategy.InMemory);
        config4.Strategy.Should().Be(RepositoryStrategy.Database);
    }
    
    [Fact]
    public void Clone_ShouldCreateDeepCopyOfConfiguration()
    {
        // Arrange
        var original = AttributeConfigurationBuilder.BuildConfiguration<CachedEntity>();
        
        // Act
        var clone = original.Clone();
        
        // Modify clone
        clone.Strategy = RepositoryStrategy.InMemory;
        clone.CacheExpirationMinutes = 60;
        clone.ConnectionPoolSize = 50;
        
        // Assert
        clone.Should().NotBeSameAs(original);
        
        // Original should be unchanged
        original.Strategy.Should().Be(RepositoryStrategy.Cached);
        original.CacheExpirationMinutes.Should().Be(30);
        original.ConnectionPoolSize.Should().Be(20);
        
        // Clone should have modified values
        clone.Strategy.Should().Be(RepositoryStrategy.InMemory);
        clone.CacheExpirationMinutes.Should().Be(60);
        clone.ConnectionPoolSize.Should().Be(50);
    }
    
    [Fact]
    public void BuildConfiguration_WithComplexAttributeCombination_ShouldMergeAllSettings()
    {
        // Arrange
        var config = AttributeConfigurationBuilder.BuildConfiguration<AuditedEntity>();
        
        // Assert - verify all attribute values are properly applied
        config.Strategy.Should().Be(RepositoryStrategy.Database);
        
        // From DatabaseRepository attribute
        config.EnableWalMode.Should().BeTrue();
        config.AutoCreateTable.Should().BeTrue();
        
        // From RepositoryAudit attribute
        config.LogQueries.Should().BeTrue();
        config.LogPerformanceMetrics.Should().BeTrue();
        config.TrackChanges.Should().BeTrue();
        config.AuditTableSuffix.Should().Be("_History");
        
        // Default values for non-specified settings
        config.BulkBatchSize.Should().Be(1000);
        config.ConnectionPoolSize.Should().Be(10);
        config.CommandTimeout.Should().Be(30);
    }
    
    [Fact]
    public void BuildConfiguration_WithNullableProperties_ShouldHandleNullsCorrectly()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<DefaultEntity>();
        
        // Assert
        config.DatabaseName.Should().BeNull();
        config.TableName.Should().BeNull();
        config.ConnectionString.Should().BeNull();
        config.ProviderName.Should().BeNull();
        config.CacheExpirationMinutes.Should().BeNull();
        config.CustomRepositoryType.Should().BeNull();
        config.CustomRepositoryArgs.Should().BeNull();
    }
    
    [Theory]
    [InlineData(typeof(SimpleEntity))]
    [InlineData(typeof(CompositeKeyEntity))]
    [InlineData(typeof(ComplexEntity))]
    [InlineData(typeof(BulkEntity))]
    [InlineData(typeof(ThreadSafeEntity))]
    [InlineData(typeof(ValidationEntity))]
    public void BuildConfiguration_WithVariousEntityTypes_ShouldNotThrow(Type entityType)
    {
        // Arrange & Act
        var buildMethod = typeof(AttributeConfigurationBuilder)
            .GetMethod(nameof(AttributeConfigurationBuilder.BuildConfiguration), new[] { typeof(Type) });
        
        // Assert
        var action = () => buildMethod?.Invoke(null, new object[] { entityType });
        action.Should().NotThrow();
    }
    
    [Fact]
    public void BuildConfiguration_WithBulkEntity_ShouldApplyBulkBatchSize()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<BulkEntity>();
        
        // Assert
        config.Should().NotBeNull();
        config.Strategy.Should().Be(RepositoryStrategy.Database);
        config.BulkBatchSize.Should().Be(1000); // Default value since attribute doesn't set it
    }
    
    [Fact]
    public void BuildConfiguration_WithPartialAttributeSettings_ShouldUseDefaultsForUnspecified()
    {
        // Arrange & Act
        var config = AttributeConfigurationBuilder.BuildConfiguration<DatabaseEntity>();
        
        // Assert
        // Specified in attribute
        config.DatabaseName.Should().Be("TestDatabase");
        config.EnableWalMode.Should().BeTrue();
        config.BulkBatchSize.Should().Be(5000);
        
        // Not specified in attribute - should use defaults
        config.ConnectionString.Should().BeNull();
        config.ProviderName.Should().BeNull();
        config.ConnectionPoolSize.Should().Be(10); // Default
        config.CommandTimeout.Should().Be(30); // Default
        config.EnableQueryCache.Should().BeFalse(); // Default
    }
}