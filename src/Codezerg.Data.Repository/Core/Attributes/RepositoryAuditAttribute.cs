using System;

namespace Codezerg.Data.Repository.Core.Attributes;

/// <summary>
/// Configures audit and logging settings for a repository
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RepositoryAuditAttribute : Attribute
{
    /// <summary>
    /// Log all queries executed by this repository
    /// </summary>
    public bool LogQueries { get; set; } = false;
    
    /// <summary>
    /// Log performance metrics for operations
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = true;
    
    /// <summary>
    /// Track changes to entities
    /// </summary>
    public bool TrackChanges { get; set; } = false;
    
    /// <summary>
    /// Suffix for audit tables
    /// </summary>
    public string AuditTableSuffix { get; set; } = "_Audit";
}