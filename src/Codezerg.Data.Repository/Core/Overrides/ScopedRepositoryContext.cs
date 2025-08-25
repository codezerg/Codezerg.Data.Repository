using System;
using System.Collections.Generic;
using Codezerg.Data.Repository.Core.Configuration;

namespace Codezerg.Data.Repository.Core.Overrides;

/// <summary>
/// Provides scoped repository configuration overrides that are automatically restored on disposal
/// </summary>
public class ScopedRepositoryContext : IScopedRepositoryContext
{
    private readonly IRepositoryOverrideManager _overrideManager;
    private readonly Dictionary<Type, RepositoryConfiguration?> _originalConfigurations = new();
    private bool _disposed;
    
    public ScopedRepositoryContext(IRepositoryOverrideManager overrideManager)
    {
        _overrideManager = overrideManager ?? throw new ArgumentNullException(nameof(overrideManager));
    }
    
    /// <summary>
    /// Override configuration for a specific entity type within this scope
    /// </summary>
    public IScopedRepositoryContext Override<T>(Action<RepositoryConfiguration> configure) where T : class, new()
    {
        ThrowIfDisposed();
        
        var entityType = typeof(T);
        
        // Save original configuration if not already saved
        if (!_originalConfigurations.ContainsKey(entityType))
        {
            _originalConfigurations[entityType] = _overrideManager.GetOverride(entityType);
        }
        
        // Apply the override
        _overrideManager.Override<T>(configure);
        
        return this;
    }
    
    /// <summary>
    /// Override configuration for a specific entity type within this scope
    /// </summary>
    public IScopedRepositoryContext OverrideFor(Type entityType, Action<RepositoryConfiguration> configure)
    {
        ThrowIfDisposed();
        
        // Save original configuration if not already saved
        if (!_originalConfigurations.ContainsKey(entityType))
        {
            _originalConfigurations[entityType] = _overrideManager.GetOverride(entityType);
        }
        
        // Apply the override
        _overrideManager.OverrideFor(entityType, configure);
        
        return this;
    }
    
    /// <summary>
    /// Create a fluent builder for configuring overrides within this scope
    /// </summary>
    public ScopedFluentOverrideBuilder ConfigureOverrides()
    {
        ThrowIfDisposed();
        return new ScopedFluentOverrideBuilder(this, _overrideManager);
    }
    
    /// <summary>
    /// Restore original configurations
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        // Restore original configurations
        foreach (var kvp in _originalConfigurations)
        {
            if (kvp.Value == null)
            {
                // There was no override before, so clear it
                _overrideManager.ClearOverride(kvp.Key);
            }
            else
            {
                // Restore the original override
                _overrideManager.OverrideFor(kvp.Key, config =>
                {
                    // Copy all properties from original
                    CopyConfiguration(kvp.Value, config);
                });
            }
        }
        
        _disposed = true;
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScopedRepositoryContext));
    }
    
    private static void CopyConfiguration(RepositoryConfiguration source, RepositoryConfiguration target)
    {
        target.Strategy = source.Strategy;
        target.DatabaseName = source.DatabaseName;
        target.TableName = source.TableName;
        target.ConnectionString = source.ConnectionString;
        target.ProviderName = source.ProviderName;
        target.BulkBatchSize = source.BulkBatchSize;
        target.EnableLogging = source.EnableLogging;
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
}

/// <summary>
/// Fluent builder for scoped overrides
/// </summary>
public class ScopedFluentOverrideBuilder : FluentOverrideBuilder
{
    private readonly IScopedRepositoryContext _context;
    
    internal ScopedFluentOverrideBuilder(IScopedRepositoryContext context, IRepositoryOverrideManager overrideManager) 
        : base(overrideManager)
    {
        _context = context;
    }
    
    /// <summary>
    /// Return to the scoped context
    /// </summary>
    public IScopedRepositoryContext EndConfiguration()
    {
        Apply();
        return _context;
    }
}