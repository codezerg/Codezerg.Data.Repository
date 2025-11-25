using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Codezerg.Data.Repository.Storage;

namespace Codezerg.Data.Repository;

/// <summary>
/// Unified repository implementation that supports in-memory, database, and cached storage modes
/// </summary>
public class Repository<T> : IRepository<T>, IDisposable where T : class, new()
{
    private readonly RepositoryOptions _options;
    private readonly IStorageStrategy<T> _storage;

    public Repository(RepositoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        var entityOps = new EntityOperations<T>();
        _storage = CreateStorage(options, entityOps);
    }

    private static IStorageStrategy<T> CreateStorage(RepositoryOptions options, EntityOperations<T> entityOps)
    {
        switch (options.Mode)
        {
            case StorageMode.InMemory:
                return new InMemoryStorage<T>(entityOps);

            case StorageMode.Database:
                return new DatabaseStorage<T>(options.ProviderName, options.ConnectionString, entityOps);

            case StorageMode.Cached:
                return new CachedStorage<T>(options.ProviderName, options.ConnectionString, entityOps);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {options.Mode}");
        }
    }

    /// <summary>
    /// Refreshes the cache from the database (Cached mode only)
    /// </summary>
    public void Refresh() => _storage.Refresh();

    /// <summary>
    /// Clears all data (InMemory and Cached modes only)
    /// </summary>
    public void Clear() => _storage.Clear();

    // Create Operations
    public T Insert(T entity) => _storage.Insert(entity);

    public IEnumerable<T> InsertRange(IEnumerable<T> entities) => _storage.InsertRange(entities);

    // Read Operations
    public IEnumerable<T> GetAll() => _storage.GetAll();

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => _storage.Find(predicate);

    public T FirstOrDefault(Expression<Func<T, bool>> predicate) => _storage.FirstOrDefault(predicate);

    public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) => _storage.Select(selector);

    public IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query) => _storage.Query(query);

    // Update Operations
    public int Update(T entity) => _storage.Update(entity);

    public int UpdateRange(IEnumerable<T> entities) => _storage.UpdateRange(entities);

    // Delete Operations
    public int Delete(T entity) => _storage.Delete(entity);

    public int DeleteRange(IEnumerable<T> entities) => _storage.DeleteRange(entities);

    public int DeleteMany(Expression<Func<T, bool>> predicate) => _storage.DeleteMany(predicate);

    // Utility Operations
    public int Count() => _storage.Count();

    public int Count(Expression<Func<T, bool>> predicate) => _storage.Count(predicate);

    public bool Exists(Expression<Func<T, bool>> predicate) => _storage.Exists(predicate);

    public void Dispose() => _storage.Dispose();
}
