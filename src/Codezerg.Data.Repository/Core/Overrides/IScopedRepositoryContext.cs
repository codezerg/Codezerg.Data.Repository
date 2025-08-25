using System;
using Codezerg.Data.Repository.Core.Configuration;

namespace Codezerg.Data.Repository.Core.Overrides;

/// <summary>
/// Interface for scoped repository configuration overrides
/// </summary>
public interface IScopedRepositoryContext : IDisposable
{
    /// <summary>
    /// Override configuration for a specific entity type within this scope
    /// </summary>
    IScopedRepositoryContext Override<T>(Action<RepositoryConfiguration> configure) where T : class, new();
    
    /// <summary>
    /// Override configuration for a specific entity type within this scope
    /// </summary>
    IScopedRepositoryContext OverrideFor(Type entityType, Action<RepositoryConfiguration> configure);
    
    /// <summary>
    /// Start fluent configuration of overrides
    /// </summary>
    ScopedFluentOverrideBuilder ConfigureOverrides();
}