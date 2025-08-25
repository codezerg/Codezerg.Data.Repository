using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Configures an entity to use in-memory repository
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class InMemoryRepositoryAttribute : RepositoryConfigurationAttribute
{
    /// <summary>
    /// Whether to persist data across repository factory instances
    /// </summary>
    public bool PersistAcrossSessions { get; set; } = false;
    
    /// <summary>
    /// Gets the repository strategy type
    /// </summary>
    public override RepositoryStrategy Strategy => RepositoryStrategy.InMemory;
}