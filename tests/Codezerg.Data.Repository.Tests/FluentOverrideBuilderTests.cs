using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Moq;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Core.Overrides;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests;

public class FluentOverrideBuilderTests
{
    private readonly Mock<IRepositoryOverrideManager> _mockOverrideManager;
    private readonly FluentOverrideBuilder _builder;
    private readonly List<(Type entityType, Action<RepositoryConfiguration> configure)> _capturedOverrides;
    
    public FluentOverrideBuilderTests()
    {
        _mockOverrideManager = new Mock<IRepositoryOverrideManager>();
        _capturedOverrides = new List<(Type, Action<RepositoryConfiguration>)>();
        
        // Capture all override calls
        _mockOverrideManager
            .Setup(x => x.OverrideFor(It.IsAny<Type>(), It.IsAny<Action<RepositoryConfiguration>>()))
            .Callback<Type, Action<RepositoryConfiguration>>((type, action) =>
            {
                _capturedOverrides.Add((type, action));
            });
        
        _builder = new FluentOverrideBuilder(_mockOverrideManager.Object);
    }
    
    [Fact]
    public void ForEntity_UseInMemory_ShouldConfigureInMemoryStrategy()
    {
        // Arrange & Act
        _builder.ForEntity<SimpleEntity>()
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        _capturedOverrides[0].entityType.Should().Be(typeof(SimpleEntity));
        
        // Verify the configuration
        var config = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config);
        config.Strategy.Should().Be(RepositoryStrategy.InMemory);
    }
    
    [Fact]
    public void ForEntity_UseInMemory_WithOptions_ShouldConfigureOptions()
    {
        // Arrange & Act
        _builder.ForEntity<InMemoryEntity>()
            .UseInMemory(options =>
            {
                options.PersistAcrossSessions = true;
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var config = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config);
        config.Strategy.Should().Be(RepositoryStrategy.InMemory);
        config.PersistAcrossSessions.Should().BeTrue();
    }
    
    [Fact]
    public void ForEntity_UseDatabase_ShouldConfigureDatabaseStrategy()
    {
        // Arrange & Act
        _builder.ForEntity<DatabaseEntity>()
            .UseDatabase(options =>
            {
                options.ConnectionString = "Data Source=test.db";
                options.ProviderName = "SQLite";
                options.EnableWalMode = true;
                options.BulkBatchSize = 5000;
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var config = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config);
        config.Strategy.Should().Be(RepositoryStrategy.Database);
        config.ConnectionString.Should().Be("Data Source=test.db");
        config.ProviderName.Should().Be("SQLite");
        config.EnableWalMode.Should().BeTrue();
        config.BulkBatchSize.Should().Be(5000);
    }
    
    [Fact]
    public void ForEntity_UseCached_ShouldConfigureCachedStrategy()
    {
        // Arrange & Act
        _builder.ForEntity<CachedEntity>()
            .UseCached(options =>
            {
                options.CacheExpirationMinutes = 60;
                options.PreloadCache = false;
                options.ConnectionString = "Data Source=cache.db";
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var config = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config);
        config.Strategy.Should().Be(RepositoryStrategy.Cached);
        config.CacheExpirationMinutes.Should().Be(60);
        config.PreloadCache.Should().BeFalse();
        config.ConnectionString.Should().Be("Data Source=cache.db");
    }
    
    [Fact]
    public void ForEntity_Configure_ShouldApplyCustomConfiguration()
    {
        // Arrange & Act
        _builder.ForEntity<ComplexEntity>()
            .Configure(config =>
            {
                config.Strategy = RepositoryStrategy.Database;
                config.EnableLogging = true;
                config.LogQueries = true;
                config.LogPerformanceMetrics = true;
                config.TrackChanges = true;
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var testConfig = new RepositoryConfiguration();
        _capturedOverrides[0].configure(testConfig);
        testConfig.Strategy.Should().Be(RepositoryStrategy.Database);
        testConfig.EnableLogging.Should().BeTrue();
        testConfig.LogQueries.Should().BeTrue();
        testConfig.LogPerformanceMetrics.Should().BeTrue();
        testConfig.TrackChanges.Should().BeTrue();
    }
    
    [Fact]
    public void ForEntity_When_WithTrueCondition_ShouldApplyOverride()
    {
        // Arrange & Act
        _builder.ForEntity<ValidationEntity>()
            .When(() => true)
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        _capturedOverrides[0].entityType.Should().Be(typeof(ValidationEntity));
    }
    
    [Fact]
    public void ForEntity_When_WithFalseCondition_ShouldNotApplyOverride()
    {
        // Arrange & Act
        _builder.ForEntity<ValidationEntity>()
            .When(() => false)
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().BeEmpty();
    }
    
    [Fact]
    public void ForEntity_When_MultipleConditions_AllMustBeTrue()
    {
        // Arrange
        var condition1 = true;
        var condition2 = true;
        var condition3 = false;
        
        // Act
        _builder.ForEntity<ThreadSafeEntity>()
            .When(() => condition1)
            .When(() => condition2)
            .When(() => condition3) // This one is false
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().BeEmpty(); // Should not apply because one condition is false
    }
    
    [Fact]
    public void ForEntity_InEnvironment_WithMatchingEnvironment_ShouldApplyOverride()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        // Act
        _builder.ForEntity<AuditedEntity>()
            .InEnvironment("Development", "Test")
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }
    
    [Fact]
    public void ForEntity_InEnvironment_WithNonMatchingEnvironment_ShouldNotApplyOverride()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        // Act
        _builder.ForEntity<AuditedEntity>()
            .InEnvironment("Development", "Test")
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().BeEmpty();
        
        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }
    
    [Fact]
    public void ForEntity_And_ShouldAllowMultipleEntityConfigurations()
    {
        // Arrange & Act
        _builder
            .ForEntity<SimpleEntity>()
                .UseInMemory()
                .And()
            .ForEntity<DatabaseEntity>()
                .UseDatabase(options => options.ConnectionString = "Data Source=db1.db")
                .And()
            .ForEntity<CachedEntity>()
                .UseCached(options => options.CacheExpirationMinutes = 30)
                .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(3);
        _capturedOverrides[0].entityType.Should().Be(typeof(SimpleEntity));
        _capturedOverrides[1].entityType.Should().Be(typeof(DatabaseEntity));
        _capturedOverrides[2].entityType.Should().Be(typeof(CachedEntity));
        
        // Verify each configuration
        var config1 = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config1);
        config1.Strategy.Should().Be(RepositoryStrategy.InMemory);
        
        var config2 = new RepositoryConfiguration();
        _capturedOverrides[1].configure(config2);
        config2.Strategy.Should().Be(RepositoryStrategy.Database);
        config2.ConnectionString.Should().Be("Data Source=db1.db");
        
        var config3 = new RepositoryConfiguration();
        _capturedOverrides[2].configure(config3);
        config3.Strategy.Should().Be(RepositoryStrategy.Cached);
        config3.CacheExpirationMinutes.Should().Be(30);
    }
    
    [Fact]
    public void ForEntity_ChainedConfiguration_ShouldApplyAllConfigurations()
    {
        // Arrange & Act
        _builder.ForEntity<ComplexEntity>()
            .UseDatabase()
            .Configure(config =>
            {
                config.ConnectionString = "Data Source=complex.db";
                config.EnableLogging = true;
            })
            .Configure(config =>
            {
                config.BulkBatchSize = 10000;
                config.CommandTimeout = 60;
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var testConfig = new RepositoryConfiguration();
        _capturedOverrides[0].configure(testConfig);
        
        testConfig.Strategy.Should().Be(RepositoryStrategy.Database);
        testConfig.ConnectionString.Should().Be("Data Source=complex.db");
        testConfig.EnableLogging.Should().BeTrue();
        testConfig.BulkBatchSize.Should().Be(10000);
        testConfig.CommandTimeout.Should().Be(60);
    }
    
    [Fact]
    public void ForEntity_ComplexConditions_ShouldEvaluateCorrectly()
    {
        // Arrange
        var isDevelopment = true;
        var isTestingEnabled = true;
        var currentHour = 14; // 2 PM
        
        // Act
        _builder.ForEntity<BulkEntity>()
            .When(() => isDevelopment && isTestingEnabled)
            .When(() => currentHour >= 9 && currentHour <= 17) // Business hours
            .InEnvironment("Development", "Staging")
            .UseInMemory(options => options.PersistAcrossSessions = false)
            .Apply();
        
        // Set environment to match
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        // Re-apply after setting environment
        _builder.Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        var config = new RepositoryConfiguration();
        _capturedOverrides[0].configure(config);
        config.Strategy.Should().Be(RepositoryStrategy.InMemory);
        config.PersistAcrossSessions.Should().BeFalse();
        
        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }
    
    [Fact]
    public void Apply_WithoutAnyConfiguration_ShouldNotThrow()
    {
        // Arrange
        var builder = new FluentOverrideBuilder(_mockOverrideManager.Object);
        
        // Act & Assert
        var action = () => builder.Apply();
        action.Should().NotThrow();
        _capturedOverrides.Should().BeEmpty();
    }
    
    [Fact]
    public void ForEntity_WithNullOverrideManager_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new FluentOverrideBuilder(null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("overrideManager");
    }
    
    [Fact]
    public void Options_Classes_ShouldModifyUnderlyingConfiguration()
    {
        // Arrange & Act
        _builder.ForEntity<DefaultEntity>()
            .UseInMemory(options =>
            {
                // Test InMemoryOptions
                options.PersistAcrossSessions = true;
                options.PersistAcrossSessions.Should().BeTrue();
            })
            .And()
            .ForEntity<SimpleEntity>()
            .UseDatabase(options =>
            {
                // Test DatabaseOptions
                options.ConnectionString = "TestConnection";
                options.ProviderName = "TestProvider";
                options.EnableWalMode = false;
                options.BulkBatchSize = 7500;
                
                options.ConnectionString.Should().Be("TestConnection");
                options.ProviderName.Should().Be("TestProvider");
                options.EnableWalMode.Should().BeFalse();
                options.BulkBatchSize.Should().Be(7500);
            })
            .And()
            .ForEntity<ThreadSafeEntity>()
            .UseCached(options =>
            {
                // Test CachedOptions
                options.CacheExpirationMinutes = 90;
                options.PreloadCache = true;
                options.ConnectionString = "CacheConnection";
                
                options.CacheExpirationMinutes.Should().Be(90);
                options.PreloadCache.Should().BeTrue();
                options.ConnectionString.Should().Be("CacheConnection");
            })
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(3);
    }
    
    [Fact]
    public void ForEntity_WithEnvironmentVariableFallback_ShouldUseCorrectEnvironment()
    {
        // Arrange - Test multiple environment variable fallbacks
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Staging");
        
        // Act
        _builder.ForEntity<ValidationEntity>()
            .InEnvironment("Staging")
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1);
        
        // Cleanup
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
    }
    
    [Fact]
    public void ForEntity_WithNoEnvironmentSet_ShouldDefaultToProduction()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
        
        // Act
        _builder.ForEntity<ValidationEntity>()
            .InEnvironment("Production")
            .UseInMemory()
            .Apply();
        
        // Assert
        _capturedOverrides.Should().HaveCount(1); // Should match because default is Production
    }
}