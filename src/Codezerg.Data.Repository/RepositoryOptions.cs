using System;

namespace Codezerg.Data.Repository;

/// <summary>
/// Configuration options for Repository{T}
/// </summary>
public class RepositoryOptions
{
    /// <summary>
    /// The storage mode for the repository
    /// </summary>
    public StorageMode Mode { get; set; }

    /// <summary>
    /// Database provider name (e.g., "SQLite" or ProviderName.SQLite from linq2db)
    /// Required for Database and Cached modes
    /// </summary>
    public string ProviderName { get; set; }

    /// <summary>
    /// Database connection string
    /// Required for Database and Cached modes
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Creates options for in-memory storage only
    /// </summary>
    public static RepositoryOptions InMemory()
    {
        return new RepositoryOptions
        {
            Mode = StorageMode.InMemory
        };
    }

    /// <summary>
    /// Creates options for direct database storage without caching
    /// </summary>
    public static RepositoryOptions Database(string providerName, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required for database mode", nameof(providerName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required for database mode", nameof(connectionString));

        return new RepositoryOptions
        {
            Mode = StorageMode.Database,
            ProviderName = providerName,
            ConnectionString = connectionString
        };
    }

    /// <summary>
    /// Creates options for cached storage (in-memory cache with database persistence)
    /// </summary>
    public static RepositoryOptions Cached(string providerName, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required for cached mode", nameof(providerName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required for cached mode", nameof(connectionString));

        return new RepositoryOptions
        {
            Mode = StorageMode.Cached,
            ProviderName = providerName,
            ConnectionString = connectionString
        };
    }

    /// <summary>
    /// Validates the options based on the selected mode
    /// </summary>
    internal void Validate()
    {
        switch (Mode)
        {
            case StorageMode.InMemory:
                // No validation needed for in-memory mode
                break;

            case StorageMode.Database:
            case StorageMode.Cached:
                if (string.IsNullOrWhiteSpace(ProviderName))
                    throw new InvalidOperationException($"ProviderName is required for {Mode} mode");
                if (string.IsNullOrWhiteSpace(ConnectionString))
                    throw new InvalidOperationException($"ConnectionString is required for {Mode} mode");
                break;

            default:
                throw new InvalidOperationException($"Unknown storage mode: {Mode}");
        }
    }
}

/// <summary>
/// Defines the storage strategy for a repository
/// </summary>
public enum StorageMode
{
    /// <summary>
    /// Store data in memory only. Data is lost when application restarts.
    /// Thread-safe with deep copy protection.
    /// Best for: unit testing, temporary data, development
    /// </summary>
    InMemory,

    /// <summary>
    /// Store data directly in database with no caching.
    /// Thread-safe through connection-per-operation pattern.
    /// Best for: write-heavy scenarios, minimal memory usage, direct database access
    /// </summary>
    Database,

    /// <summary>
    /// Store data in database with full in-memory caching.
    /// All data loaded on initialization. Writes go to database then update cache.
    /// Thread-safe with coordinated memory and database operations.
    /// Best for: read-heavy scenarios, small to medium datasets, optimal query performance
    /// </summary>
    Cached
}
