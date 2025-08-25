using System;

namespace Codezerg.Data.Repository.Core;

/// <summary>
/// Configuration options for repositories
/// </summary>
public class RepositoryOptions
{
    /// <summary>
    /// The base path for database files. Defaults to {AppDomain.BaseDirectory}/Data
    /// </summary>
    public string DataPath { get; set; } = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    
    /// <summary>
    /// Connection string template. Use {DataPath} and {DatabaseName} as placeholders.
    /// </summary>
    public string ConnectionStringTemplate { get; set; } = "Data Source={DataPath}/{DatabaseName}.db";
    
    /// <summary>
    /// The database provider name. Defaults to SQLite.
    /// </summary>
    public string ProviderName { get; set; } = LinqToDB.ProviderName.SQLite;
    
    /// <summary>
    /// Whether to automatically create tables if they don't exist.
    /// </summary>
    public bool AutoCreateTables { get; set; } = true;
    
    /// <summary>
    /// Whether to enable WAL mode for SQLite databases.
    /// </summary>
    public bool EnableWalMode { get; set; } = true;
    
    /// <summary>
    /// Batch size for bulk operations.
    /// </summary>
    public int BulkOperationBatchSize { get; set; } = 1000;
    
    /// <summary>
    /// Whether to use the cached repository by default.
    /// </summary>
    public bool UseCachedRepository { get; set; } = true;
    
    /// <summary>
    /// Custom database name resolver. If null, uses assembly name.
    /// </summary>
    public Func<Type, string>? DatabaseNameResolver { get; set; }
    
    /// <summary>
    /// Custom table name resolver. If null, uses type name or TableAttribute.
    /// </summary>
    public Func<Type, string>? TableNameResolver { get; set; }
    
    /// <summary>
    /// Whether to use attribute-based configuration
    /// </summary>
    public bool UseAttributeConfiguration { get; set; } = false;
}