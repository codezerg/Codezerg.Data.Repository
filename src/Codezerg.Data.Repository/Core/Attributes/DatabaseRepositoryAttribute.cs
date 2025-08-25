using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Configures an entity to use database repository
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DatabaseRepositoryAttribute : RepositoryConfigurationAttribute
{
    /// <summary>
    /// Custom connection string for this entity
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Database provider name (e.g., SQLite, SqlServer)
    /// </summary>
    public string? ProviderName { get; set; }
    
    /// <summary>
    /// Enable WAL mode for SQLite databases
    /// </summary>
    public bool EnableWalMode { get; set; } = true;
    
    /// <summary>
    /// Automatically create table if it doesn't exist
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;
    
    /// <summary>
    /// Gets the repository strategy type
    /// </summary>
    public override RepositoryStrategy Strategy => RepositoryStrategy.Database;
}