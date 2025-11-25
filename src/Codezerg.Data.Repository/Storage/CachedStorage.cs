using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Codezerg.Data.Repository.Migration;

namespace Codezerg.Data.Repository.Storage;

/// <summary>
/// Hybrid storage implementation combining in-memory caching with database persistence
/// </summary>
internal sealed class CachedStorage<T> : IStorageStrategy<T> where T : class, new()
{
    private readonly List<T> _entities;
    private readonly ReaderWriterLockSlim _lock;
    private readonly string _providerName;
    private readonly string _connectionString;
    private readonly MappingSchema _mappingSchema;
    private readonly EntityOperations<T> _entityOps;
    private bool _isInitialized;

    public CachedStorage(string providerName, string connectionString, EntityOperations<T> entityOps)
    {
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _entityOps = entityOps ?? throw new ArgumentNullException(nameof(entityOps));
        _mappingSchema = EntityMapping<T>.GetMappingSchema();
        _entities = new List<T>();
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        _isInitialized = false;

        InitializeDatabase();
        LoadDataFromDatabase();
    }

    private void InitializeDatabase()
    {
        using (var db = CreateConnection())
        {
            SchemaManager<T>.EnsureSchema(db, _mappingSchema);
        }
    }

    private DataConnection CreateConnection()
    {
        var db = new DataConnection(_providerName, _connectionString, _mappingSchema);
        if (db.DataProvider.Name.ToLowerInvariant().Contains("sqlite"))
        {
            db.Execute("pragma journal_mode = WAL;");
        }
        return db;
    }

    private void LoadDataFromDatabase()
    {
        _lock.EnterWriteLock();
        try
        {
            _entities.Clear();
            _entityOps.IdentityManager.ResetIdentitySeed();

            using (var db = CreateConnection())
            {
                try
                {
                    var allEntities = db.GetTable<T>().ToList();
                    foreach (var entity in allEntities)
                    {
                        var entityCopy = _entityOps.CreateDeepCopy(entity);
                        _entities.Add(entityCopy);
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("no such table"))
                {
                    // Table doesn't exist yet - will be created on first insert
                }
            }

            _isInitialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            LoadDataFromDatabase();
        }
    }

    public T Insert(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                if (_entityOps.IdentityManager.IdentityProperty != null)
                {
                    var id = db.InsertWithIdentity(entity);
                    var propertyType = _entityOps.IdentityManager.IdentityProperty.PropertyType;
                    _entityOps.IdentityManager.IdentityProperty.SetValue(entity,
                        Convert.ChangeType(id, propertyType));

                    var entityCopy = _entityOps.CreateDeepCopy(entity);
                    _entities.Add(entityCopy);
                }
                else
                {
                    var dbResult = db.Insert(entity);
                    if (dbResult > 0)
                    {
                        var entityCopy = _entityOps.CreateDeepCopy(entity);
                        _entities.Add(entityCopy);
                    }
                }

                return entity;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<T> InsertRange(IEnumerable<T> entities)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            var entitiesList = entities.ToList();

            using (var db = CreateConnection())
            {
                foreach (var entity in entitiesList)
                {
                    if (_entityOps.IdentityManager.IdentityProperty != null)
                    {
                        var id = db.InsertWithIdentity(entity);
                        var propertyType = _entityOps.IdentityManager.IdentityProperty.PropertyType;
                        _entityOps.IdentityManager.IdentityProperty.SetValue(entity,
                            Convert.ChangeType(id, propertyType));

                        var entityCopy = _entityOps.CreateDeepCopy(entity);
                        _entities.Add(entityCopy);
                    }
                    else
                    {
                        var dbResult = db.Insert(entity);
                        if (dbResult > 0)
                        {
                            var entityCopy = _entityOps.CreateDeepCopy(entity);
                            _entities.Add(entityCopy);
                        }
                    }
                }
                return entitiesList;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<T> GetAll()
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            return _entities.Select(_entityOps.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            return _entities.Where(compiledPredicate).Select(_entityOps.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public T FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            var entity = _entities.FirstOrDefault(compiledPredicate);
            return entity != null ? _entityOps.CreateDeepCopy(entity) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledSelector = selector.Compile();
            return _entities.Select(compiledSelector).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var copiedQueryable = _entities.Select(_entityOps.CreateDeepCopy).AsQueryable();
            return query(copiedQueryable).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int Update(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var dbResult = db.Update(entity);

                if (dbResult > 0 && _entityOps.PrimaryKeyProperties.Count > 0)
                {
                    var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
                    if (existingEntity != null)
                    {
                        var entityCopy = _entityOps.CreateDeepCopy(entity);
                        _entityOps.UpdateEntityValues(existingEntity, entityCopy);
                    }
                }

                return dbResult;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int UpdateRange(IEnumerable<T> entities)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            var entitiesList = entities.ToList();

            using (var db = CreateConnection())
            {
                var count = 0;
                foreach (var entity in entitiesList)
                {
                    var dbResult = db.Update(entity);

                    if (dbResult > 0 && _entityOps.PrimaryKeyProperties.Count > 0)
                    {
                        var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
                        if (existingEntity != null)
                        {
                            var entityCopy = _entityOps.CreateDeepCopy(entity);
                            _entityOps.UpdateEntityValues(existingEntity, entityCopy);
                            count++;
                        }
                    }
                }
                return count;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int Delete(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var dbResult = db.Delete(entity);

                if (dbResult > 0 && _entityOps.PrimaryKeyProperties.Count > 0)
                {
                    var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
                    if (existingEntity != null)
                    {
                        _entities.Remove(existingEntity);
                    }
                }

                return dbResult;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int DeleteRange(IEnumerable<T> entities)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            var entitiesList = entities.ToList();

            using (var db = CreateConnection())
            {
                var count = 0;
                foreach (var entity in entitiesList)
                {
                    var dbResult = db.Delete(entity);

                    if (dbResult > 0 && _entityOps.PrimaryKeyProperties.Count > 0)
                    {
                        var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
                        if (existingEntity != null)
                        {
                            _entities.Remove(existingEntity);
                            count++;
                        }
                    }
                }
                return count;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int DeleteMany(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var count = db.GetTable<T>().Where(predicate).Delete();

                if (count > 0)
                {
                    var compiledPredicate = predicate.Compile();
                    var matchingEntities = _entities.Where(compiledPredicate).ToList();
                    foreach (var entity in matchingEntities)
                    {
                        _entities.Remove(entity);
                    }
                }

                return count;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int Count()
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            return _entities.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int Count(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            return _entities.Count(compiledPredicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool Exists(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            return _entities.Any(compiledPredicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _entities.Clear();
            _entityOps.IdentityManager.ResetIdentitySeed();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Refresh()
    {
        LoadDataFromDatabase();
    }

    public void Dispose()
    {
        _lock?.Dispose();
        // Clear SQLite connection pools to release file locks
        ClearConnectionPool();
    }

    private static void ClearConnectionPool()
    {
        try
        {
            // Use reflection to call SqliteConnection.ClearAllPools() if available
            var sqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
            if (sqliteConnectionType != null)
            {
                var clearMethod = sqliteConnectionType.GetMethod("ClearAllPools",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                clearMethod?.Invoke(null, null);
            }
        }
        catch
        {
            // Ignore errors - pool clearing is best-effort
        }
    }
}
