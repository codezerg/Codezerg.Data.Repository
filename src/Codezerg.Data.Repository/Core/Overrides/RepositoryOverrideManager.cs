using System;
using System.Collections.Concurrent;
using Codezerg.Data.Repository.Core.Configuration;

namespace Codezerg.Data.Repository.Core.Overrides;

/// <summary>
/// Default implementation of repository override manager
/// </summary>
public class RepositoryOverrideManager : IRepositoryOverrideManager
{
    private readonly ConcurrentDictionary<Type, RepositoryConfiguration> _overrides = new();
    
    /// <inheritdoc/>
    public void Override<T>(Action<RepositoryConfiguration> configure) where T : class, new()
    {
        OverrideFor(typeof(T), configure);
    }
    
    /// <inheritdoc/>
    public void OverrideFor(Type entityType, Action<RepositoryConfiguration> configure)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
        
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
        
        // Start with existing override or attribute-based configuration
        var config = GetOverride(entityType) ?? 
                    AttributeConfigurationBuilder.BuildConfiguration(entityType);
        
        // Apply the override
        configure(config);
        
        // Store the override
        _overrides.AddOrUpdate(entityType, config, (_, __) => config);
    }
    
    /// <inheritdoc/>
    public RepositoryConfiguration? GetOverride(Type entityType)
    {
        return _overrides.TryGetValue(entityType, out var config) ? config.Clone() : null;
    }
    
    /// <inheritdoc/>
    public bool HasOverride(Type entityType)
    {
        return _overrides.ContainsKey(entityType);
    }
    
    /// <inheritdoc/>
    public void ClearOverride<T>() where T : class, new()
    {
        ClearOverride(typeof(T));
    }
    
    /// <inheritdoc/>
    public void ClearOverride(Type entityType)
    {
        _overrides.TryRemove(entityType, out _);
    }
    
    /// <inheritdoc/>
    public void ClearAllOverrides()
    {
        _overrides.Clear();
    }
}