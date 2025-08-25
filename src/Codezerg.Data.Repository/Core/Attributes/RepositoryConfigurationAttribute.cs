using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Base attribute for repository configuration
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public abstract class RepositoryConfigurationAttribute : Attribute
{
    /// <summary>
    /// Custom database name for this entity
    /// </summary>
    public string? DatabaseName { get; set; }
    
    /// <summary>
    /// Custom table name for this entity
    /// </summary>
    public string? TableName { get; set; }
    
    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int? BulkBatchSize { get; set; }
    
    /// <summary>
    /// Enable logging for this repository
    /// </summary>
    public bool? EnableLogging { get; set; }
    
    /// <summary>
    /// Gets the repository strategy type
    /// </summary>
    public abstract RepositoryStrategy Strategy { get; }
}

/// <summary>
/// Repository strategy types
/// </summary>
public enum RepositoryStrategy
{
    /// <summary>
    /// Use cached repository (default)
    /// </summary>
    Cached,
    
    /// <summary>
    /// Use in-memory repository
    /// </summary>
    InMemory,
    
    /// <summary>
    /// Use database repository
    /// </summary>
    Database,
    
    /// <summary>
    /// Use custom repository implementation
    /// </summary>
    Custom
}