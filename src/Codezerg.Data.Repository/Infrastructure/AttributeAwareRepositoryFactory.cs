using System;
using System.Collections.Concurrent;
using System.IO;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;
using Codezerg.Data.Repository.Core.Overrides;
using Codezerg.Data.Repository.Implementations;
using Codezerg.Data.Repository.Infrastructure.Diagnostics;

namespace Codezerg.Data.Repository.Infrastructure;

/// <summary>
/// Repository factory that supports attribute-based configuration
/// </summary>
public class AttributeAwareRepositoryFactory : IRepositoryFactory
{
    private readonly RepositoryOptions _defaultOptions;
    private readonly IRepositoryOverrideManager? _overrideManager;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ConcurrentDictionary<Type, object> _singletonRepositories = new();
    
    public AttributeAwareRepositoryFactory(
        RepositoryOptions? defaultOptions = null,
        IRepositoryOverrideManager? overrideManager = null,
        ILoggerFactory? loggerFactory = null)
    {
        _defaultOptions = defaultOptions ?? new RepositoryOptions();
        _overrideManager = overrideManager;
        _loggerFactory = loggerFactory;
    }
    
    public IRepository<T> GetRepository<T>() where T : class, new()
    {
        var entityType = typeof(T);
        
        // Build configuration from multiple sources
        var config = BuildFinalConfiguration<T>();
        
        // Check for singleton repositories (InMemory with PersistAcrossSessions)
        if (config.Strategy == RepositoryStrategy.InMemory && config.PersistAcrossSessions)
        {
            return (IRepository<T>)_singletonRepositories.GetOrAdd(entityType, _ => CreateRepository<T>(config));
        }
        
        // Create new repository instance
        return CreateRepository<T>(config);
    }
    
    private RepositoryConfiguration BuildFinalConfiguration<T>() where T : class
    {
        var entityType = typeof(T);
        
        // Start with default options
        var config = new RepositoryConfiguration
        {
            Strategy = _defaultOptions.UseCachedRepository ? RepositoryStrategy.Cached : RepositoryStrategy.Database,
            DatabaseName = _defaultOptions.DatabaseNameResolver?.Invoke(entityType) ?? EntityMapping<T>.GetDatabaseName(),
            TableName = _defaultOptions.TableNameResolver?.Invoke(entityType) ?? EntityMapping<T>.GetTableName(),
            ProviderName = _defaultOptions.ProviderName,
            BulkBatchSize = _defaultOptions.BulkOperationBatchSize,
            EnableWalMode = _defaultOptions.EnableWalMode,
            AutoCreateTable = _defaultOptions.AutoCreateTables
        };
        
        // Apply attribute configuration
        var attributeConfig = AttributeConfigurationBuilder.BuildConfiguration<T>();
        MergeConfiguration(config, attributeConfig);
        
        // Apply runtime overrides if available
        if (_overrideManager?.HasOverride(entityType) == true)
        {
            var overrideConfig = _overrideManager.GetOverride(entityType)!;
            MergeConfiguration(config, overrideConfig);
        }
        
        // Apply environment variable overrides
        ApplyEnvironmentVariableOverrides<T>(config);
        
        // Build connection string if needed
        if (string.IsNullOrEmpty(config.ConnectionString) && 
            (config.Strategy == RepositoryStrategy.Database || config.Strategy == RepositoryStrategy.Cached))
        {
            config.ConnectionString = BuildConnectionString(config);
        }
        
        return config;
    }
    
    private IRepository<T> CreateRepository<T>(RepositoryConfiguration config) where T : class, new()
    {
        IRepository<T> repository = config.Strategy switch
        {
            RepositoryStrategy.InMemory => new InMemoryRepository<T>(),
            
            RepositoryStrategy.Database => new DatabaseRepository<T>(
                config.ProviderName ?? ProviderName.SQLite,
                config.ConnectionString!),
            
            RepositoryStrategy.Cached => new CachedRepository<T>(
                config.ProviderName ?? ProviderName.SQLite,
                config.ConnectionString!),
            
            RepositoryStrategy.Custom when config.CustomRepositoryType != null => 
                CreateCustomRepository<T>(config),
            
            _ => throw new InvalidOperationException($"Unsupported repository strategy: {config.Strategy}")
        };
        
        // Apply decorators if needed
        if (config.EnableLogging && _loggerFactory != null)
        {
            var logger = _loggerFactory.CreateLogger<RepositoryLoggingDecorator<T>>();
            repository = new RepositoryLoggingDecorator<T>(repository, logger, config.LogPerformanceMetrics);
        }
        
        return repository;
    }
    
    private IRepository<T> CreateCustomRepository<T>(RepositoryConfiguration config) where T : class, new()
    {
        if (config.CustomRepositoryType == null)
            throw new InvalidOperationException("Custom repository type not specified");
        
        var args = config.CustomRepositoryArgs ?? Array.Empty<object>();
        var instance = Activator.CreateInstance(config.CustomRepositoryType, args);
        
        if (instance is not IRepository<T> repository)
            throw new InvalidOperationException($"Type {config.CustomRepositoryType} does not implement IRepository<{typeof(T).Name}>");
        
        return repository;
    }
    
    private void MergeConfiguration(RepositoryConfiguration target, RepositoryConfiguration source)
    {
        // Only override if source has explicit values
        if (source.Strategy != target.Strategy)
            target.Strategy = source.Strategy;
        
        if (!string.IsNullOrEmpty(source.DatabaseName))
            target.DatabaseName = source.DatabaseName;
        
        if (!string.IsNullOrEmpty(source.TableName))
            target.TableName = source.TableName;
        
        if (!string.IsNullOrEmpty(source.ConnectionString))
            target.ConnectionString = source.ConnectionString;
        
        if (!string.IsNullOrEmpty(source.ProviderName))
            target.ProviderName = source.ProviderName;
        
        if (source.BulkBatchSize != 1000) // Check for non-default
            target.BulkBatchSize = source.BulkBatchSize;
        
        if (source.EnableLogging)
            target.EnableLogging = source.EnableLogging;
        
        // Merge other properties...
        target.PersistAcrossSessions = source.PersistAcrossSessions;
        target.EnableWalMode = source.EnableWalMode;
        target.AutoCreateTable = source.AutoCreateTable;
        target.CacheExpirationMinutes = source.CacheExpirationMinutes;
        target.PreloadCache = source.PreloadCache;
        target.ConnectionPoolSize = source.ConnectionPoolSize;
        target.CommandTimeout = source.CommandTimeout;
        target.EnableQueryCache = source.EnableQueryCache;
        target.QueryCacheDurationSeconds = source.QueryCacheDurationSeconds;
        target.LogQueries = source.LogQueries;
        target.LogPerformanceMetrics = source.LogPerformanceMetrics;
        target.TrackChanges = source.TrackChanges;
        target.AuditTableSuffix = source.AuditTableSuffix;
        target.CustomRepositoryType = source.CustomRepositoryType;
        target.CustomRepositoryArgs = source.CustomRepositoryArgs;
    }
    
    private void ApplyEnvironmentVariableOverrides<T>(RepositoryConfiguration config)
    {
        var typeName = typeof(T).Name;
        var prefix = $"REPO_OVERRIDE_{typeName}__";
        
        // Check for strategy override
        var strategyEnv = Environment.GetEnvironmentVariable($"{prefix}Strategy");
        if (!string.IsNullOrEmpty(strategyEnv) && Enum.TryParse<RepositoryStrategy>(strategyEnv, out var strategy))
        {
            config.Strategy = strategy;
        }
        
        // Check for other overrides
        var connectionStringEnv = Environment.GetEnvironmentVariable($"{prefix}ConnectionString");
        if (!string.IsNullOrEmpty(connectionStringEnv))
        {
            config.ConnectionString = connectionStringEnv;
        }
        
        var enableLoggingEnv = Environment.GetEnvironmentVariable($"{prefix}EnableLogging");
        if (!string.IsNullOrEmpty(enableLoggingEnv) && bool.TryParse(enableLoggingEnv, out var enableLogging))
        {
            config.EnableLogging = enableLogging;
        }
        
        // Add more environment variable checks as needed...
    }
    
    private string BuildConnectionString(RepositoryConfiguration config)
    {
        // Ensure data folder exists
        if (!Directory.Exists(_defaultOptions.DataPath))
        {
            Directory.CreateDirectory(_defaultOptions.DataPath);
        }
        
        var connectionString = _defaultOptions.ConnectionStringTemplate
            .Replace("{DataPath}", _defaultOptions.DataPath)
            .Replace("{DatabaseName}", config.DatabaseName ?? "Default");
        
        return connectionString;
    }
}