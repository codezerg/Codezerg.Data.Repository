using System;
using Codezerg.Data.Repository.Core.Configuration;

namespace Codezerg.Data.Repository.Core.Overrides;

/// <summary>
/// Manages runtime overrides for repository configurations
/// </summary>
public interface IRepositoryOverrideManager
{
    /// <summary>
    /// Override configuration for a specific entity type
    /// </summary>
    void Override<T>(Action<RepositoryConfiguration> configure) where T : class, new();
    
    /// <summary>
    /// Override configuration for a specific entity type
    /// </summary>
    void OverrideFor(Type entityType, Action<RepositoryConfiguration> configure);
    
    /// <summary>
    /// Get override configuration for a type if exists
    /// </summary>
    RepositoryConfiguration? GetOverride(Type entityType);
    
    /// <summary>
    /// Check if an override exists for a type
    /// </summary>
    bool HasOverride(Type entityType);
    
    /// <summary>
    /// Clear override for a specific entity type
    /// </summary>
    void ClearOverride<T>() where T : class, new();
    
    /// <summary>
    /// Clear override for a specific entity type
    /// </summary>
    void ClearOverride(Type entityType);
    
    /// <summary>
    /// Clear all overrides
    /// </summary>
    void ClearAllOverrides();
}