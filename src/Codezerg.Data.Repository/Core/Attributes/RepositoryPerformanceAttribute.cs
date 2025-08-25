using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Configures performance settings for a repository
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RepositoryPerformanceAttribute : Attribute
{
    /// <summary>
    /// Connection pool size for database connections
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 10;
    
    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// Enable query result caching
    /// </summary>
    public bool EnableQueryCache { get; set; } = false;
    
    /// <summary>
    /// Query cache duration in seconds
    /// </summary>
    public int QueryCacheDurationSeconds { get; set; } = 60;
}