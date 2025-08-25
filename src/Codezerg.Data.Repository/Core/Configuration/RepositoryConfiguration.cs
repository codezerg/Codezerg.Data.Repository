using System;
using Codezerg.Data.Repository.Core.Attributes;

namespace Codezerg.Data.Repository.Core.Configuration;

/// <summary>
/// Complete configuration for a repository
/// </summary>
public class RepositoryConfiguration
{
    /// <summary>
    /// Repository strategy to use
    /// </summary>
    public RepositoryStrategy Strategy { get; set; } = RepositoryStrategy.Cached;
    
    /// <summary>
    /// Database name
    /// </summary>
    public string? DatabaseName { get; set; }
    
    /// <summary>
    /// Table name
    /// </summary>
    public string? TableName { get; set; }
    
    /// <summary>
    /// Connection string
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Provider name (e.g., SQLite, SqlServer)
    /// </summary>
    public string? ProviderName { get; set; }
    
    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BulkBatchSize { get; set; } = 1000;
    
    /// <summary>
    /// Enable logging
    /// </summary>
    public bool EnableLogging { get; set; } = false;
    
    // InMemory specific
    /// <summary>
    /// Persist in-memory data across sessions
    /// </summary>
    public bool PersistAcrossSessions { get; set; } = false;
    
    // Database specific
    /// <summary>
    /// Enable WAL mode for SQLite
    /// </summary>
    public bool EnableWalMode { get; set; } = true;
    
    /// <summary>
    /// Auto-create tables if they don't exist
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;
    
    // Cached specific
    /// <summary>
    /// Cache expiration in minutes
    /// </summary>
    public int? CacheExpirationMinutes { get; set; }
    
    /// <summary>
    /// Preload cache on initialization
    /// </summary>
    public bool PreloadCache { get; set; } = true;
    
    // Performance settings
    /// <summary>
    /// Connection pool size
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 10;
    
    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
    
    /// <summary>
    /// Enable query caching
    /// </summary>
    public bool EnableQueryCache { get; set; } = false;
    
    /// <summary>
    /// Query cache duration in seconds
    /// </summary>
    public int QueryCacheDurationSeconds { get; set; } = 60;
    
    // Audit settings
    /// <summary>
    /// Log queries
    /// </summary>
    public bool LogQueries { get; set; } = false;
    
    /// <summary>
    /// Log performance metrics
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Track entity changes
    /// </summary>
    public bool TrackChanges { get; set; } = false;
    
    /// <summary>
    /// Audit table suffix
    /// </summary>
    public string AuditTableSuffix { get; set; } = "_Audit";
    
    /// <summary>
    /// Custom repository type (for Strategy = Custom)
    /// </summary>
    public Type? CustomRepositoryType { get; set; }
    
    /// <summary>
    /// Constructor arguments for custom repository
    /// </summary>
    public object[]? CustomRepositoryArgs { get; set; }
    
    /// <summary>
    /// Creates a copy of this configuration
    /// </summary>
    public RepositoryConfiguration Clone()
    {
        return new RepositoryConfiguration
        {
            Strategy = Strategy,
            DatabaseName = DatabaseName,
            TableName = TableName,
            ConnectionString = ConnectionString,
            ProviderName = ProviderName,
            BulkBatchSize = BulkBatchSize,
            EnableLogging = EnableLogging,
            PersistAcrossSessions = PersistAcrossSessions,
            EnableWalMode = EnableWalMode,
            AutoCreateTable = AutoCreateTable,
            CacheExpirationMinutes = CacheExpirationMinutes,
            PreloadCache = PreloadCache,
            ConnectionPoolSize = ConnectionPoolSize,
            CommandTimeout = CommandTimeout,
            EnableQueryCache = EnableQueryCache,
            QueryCacheDurationSeconds = QueryCacheDurationSeconds,
            LogQueries = LogQueries,
            LogPerformanceMetrics = LogPerformanceMetrics,
            TrackChanges = TrackChanges,
            AuditTableSuffix = AuditTableSuffix,
            CustomRepositoryType = CustomRepositoryType,
            CustomRepositoryArgs = CustomRepositoryArgs
        };
    }
}