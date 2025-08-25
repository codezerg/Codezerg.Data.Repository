using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Codezerg.Data.Repository.Core.Attributes;

namespace Codezerg.Data.Repository.Core.Configuration;

/// <summary>
/// Builds repository configuration from attributes
/// </summary>
public class AttributeConfigurationBuilder
{
    private static readonly ConcurrentDictionary<Type, RepositoryConfiguration> _cache = new();
    
    /// <summary>
    /// Build configuration for an entity type from its attributes
    /// </summary>
    public static RepositoryConfiguration BuildConfiguration<T>() where T : class
    {
        return BuildConfiguration(typeof(T));
    }
    
    /// <summary>
    /// Build configuration for an entity type from its attributes
    /// </summary>
    public static RepositoryConfiguration BuildConfiguration(Type entityType)
    {
        // Check cache first
        if (_cache.TryGetValue(entityType, out var cached))
        {
            return cached.Clone();
        }
        
        var config = new RepositoryConfiguration();
        
        // Get all attributes from the entity type
        var attributes = entityType.GetCustomAttributes(true);
        
        // Process repository configuration attributes
        var repoConfigAttr = attributes.OfType<RepositoryConfigurationAttribute>().FirstOrDefault();
        if (repoConfigAttr != null)
        {
            ApplyRepositoryConfigurationAttribute(config, repoConfigAttr);
        }
        
        // Process performance attributes
        var perfAttr = attributes.OfType<RepositoryPerformanceAttribute>().FirstOrDefault();
        if (perfAttr != null)
        {
            ApplyPerformanceAttribute(config, perfAttr);
        }
        
        // Process audit attributes
        var auditAttr = attributes.OfType<RepositoryAuditAttribute>().FirstOrDefault();
        if (auditAttr != null)
        {
            ApplyAuditAttribute(config, auditAttr);
        }
        
        // Cache the configuration
        _cache.TryAdd(entityType, config.Clone());
        
        return config;
    }
    
    private static void ApplyRepositoryConfigurationAttribute(RepositoryConfiguration config, RepositoryConfigurationAttribute attr)
    {
        config.Strategy = attr.Strategy;
        
        if (attr.DatabaseName != null)
            config.DatabaseName = attr.DatabaseName;
        
        if (attr.TableName != null)
            config.TableName = attr.TableName;
        
        if (attr.BulkBatchSize.HasValue)
            config.BulkBatchSize = attr.BulkBatchSize.Value;
        
        if (attr.EnableLogging.HasValue)
            config.EnableLogging = attr.EnableLogging.Value;
        
        // Apply specific attribute properties
        switch (attr)
        {
            case InMemoryRepositoryAttribute inMemory:
                config.PersistAcrossSessions = inMemory.PersistAcrossSessions;
                break;
                
            case DatabaseRepositoryAttribute database:
                if (database.ConnectionString != null)
                    config.ConnectionString = database.ConnectionString;
                if (database.ProviderName != null)
                    config.ProviderName = database.ProviderName;
                config.EnableWalMode = database.EnableWalMode;
                config.AutoCreateTable = database.AutoCreateTable;
                break;
                
            case CachedRepositoryAttribute cached:
                config.CacheExpirationMinutes = cached.CacheExpirationMinutes;
                config.PreloadCache = cached.PreloadCache;
                if (cached.ConnectionString != null)
                    config.ConnectionString = cached.ConnectionString;
                if (cached.ProviderName != null)
                    config.ProviderName = cached.ProviderName;
                break;
        }
    }
    
    private static void ApplyPerformanceAttribute(RepositoryConfiguration config, RepositoryPerformanceAttribute attr)
    {
        config.ConnectionPoolSize = attr.ConnectionPoolSize;
        config.CommandTimeout = attr.CommandTimeout;
        config.EnableQueryCache = attr.EnableQueryCache;
        config.QueryCacheDurationSeconds = attr.QueryCacheDurationSeconds;
    }
    
    private static void ApplyAuditAttribute(RepositoryConfiguration config, RepositoryAuditAttribute attr)
    {
        config.LogQueries = attr.LogQueries;
        config.LogPerformanceMetrics = attr.LogPerformanceMetrics;
        config.TrackChanges = attr.TrackChanges;
        config.AuditTableSuffix = attr.AuditTableSuffix;
    }
    
    /// <summary>
    /// Clear the configuration cache
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
    
    /// <summary>
    /// Clear cache for a specific type
    /// </summary>
    public static void ClearCache(Type entityType)
    {
        _cache.TryRemove(entityType, out _);
    }
}