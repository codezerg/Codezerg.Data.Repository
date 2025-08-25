using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Configures an entity to use cached repository
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CachedRepositoryAttribute : RepositoryConfigurationAttribute
{
    /// <summary>
    /// Cache expiration time in minutes (null = no expiration)
    /// </summary>
    public int? CacheExpirationMinutes { get; set; }
    
    /// <summary>
    /// Whether to preload all data into cache on initialization
    /// </summary>
    public bool PreloadCache { get; set; } = true;
    
    /// <summary>
    /// Custom connection string for this entity
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Database provider name
    /// </summary>
    public string? ProviderName { get; set; }
    
    /// <summary>
    /// Gets the repository strategy type
    /// </summary>
    public override RepositoryStrategy Strategy => RepositoryStrategy.Cached;
}