using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Codezerg.Data.Repository.Migration;

namespace Codezerg.Data.Repository;

/// <summary>
/// Unified repository implementation that supports in-memory, database, and cached storage modes
/// </summary>
public class Repository<T> : IRepository<T>, IDisposable where T : class, new()
{
    private readonly RepositoryOptions _options;
    private readonly EntityOperations<T> _entity;

    // In-memory storage
    private readonly List<T> _entities;
    private readonly ReaderWriterLockSlim _lock;

    // Database storage
    private readonly string _providerName;
    private readonly string _connectionString;
    private readonly MappingSchema _mappingSchema;

    // Cached mode
    private bool _isInitialized = false;

    public Repository(RepositoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _entity = new EntityOperations<T>();

        // Initialize based on storage mode
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                _entities = new List<T>();
                _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                break;

            case StorageMode.Database:
                _providerName = _options.ProviderName;
                _connectionString = _options.ConnectionString;
                _mappingSchema = EntityMapping<T>.GetMappingSchema();
                InitializeDatabase();
                break;

            case StorageMode.Cached:
                _entities = new List<T>();
                _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _providerName = _options.ProviderName;
                _connectionString = _options.ConnectionString;
                _mappingSchema = EntityMapping<T>.GetMappingSchema();
                InitializeDatabase();
                LoadDataFromDatabase();
                break;

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private void InitializeDatabase()
    {
        try
        {
            using (var db = CreateConnection())
            {
                SchemaManager<T>.EnsureSchema(db, _mappingSchema);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
            throw;
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
        try
        {
            _lock.EnterWriteLock();

            // Clear any existing data
            _entities.Clear();
            _entity.IdentityManager.ResetIdentitySeed();

            // Load all data from database
            using (var db = CreateConnection())
            {
                try
                {
                    var allEntities = db.GetTable<T>().ToList();

                    // Add to in-memory storage with deep copies
                    foreach (var entity in allEntities)
                    {
                        var entityCopy = _entity.CreateDeepCopy(entity);
                        _entities.Add(entityCopy);
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("no such table"))
                {
                    // Table doesn't exist yet - this is fine for a new database
                    // The table will be created on first insert
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
        if (_options.Mode == StorageMode.Cached && !_isInitialized)
        {
            LoadDataFromDatabase();
        }
    }

    /// <summary>
    /// Refreshes the cache from the database (Cached mode only)
    /// </summary>
    public void Refresh()
    {
        if (_options.Mode != StorageMode.Cached)
            throw new InvalidOperationException("Refresh is only supported in Cached mode");

        LoadDataFromDatabase();
    }

    /// <summary>
    /// Clears all data (InMemory and Cached modes only)
    /// </summary>
    public void Clear()
    {
        if (_options.Mode == StorageMode.Database)
            throw new InvalidOperationException("Clear is not supported in Database mode. Use DeleteMany or database operations.");

        _lock.EnterWriteLock();
        try
        {
            _entities.Clear();
            _entity.IdentityManager.ResetIdentitySeed();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Create Operations
    public int Insert(T entity)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return InsertInMemory(entity);

            case StorageMode.Database:
                return InsertDatabase(entity);

            case StorageMode.Cached:
                return InsertCached(entity);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int InsertInMemory(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var entityCopy = _entity.PrepareForInsert(entity);
            _entities.Add(entityCopy);
            _entity.CopyIdentityValue(entityCopy, entity);
            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int InsertDatabase(T entity)
    {
        using (var db = CreateConnection())
        {
            return db.Insert(entity);
        }
    }

    private int InsertCached(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            // Insert to database first
            using (var db = CreateConnection())
            {
                var dbResult = db.Insert(entity);

                if (dbResult > 0)
                {
                    // If successful, insert to memory
                    var entityCopy = _entity.CreateDeepCopy(entity);
                    _entities.Add(entityCopy);
                }

                return dbResult;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int InsertWithIdentity(T entity)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return InsertWithIdentityInMemory(entity);

            case StorageMode.Database:
                return InsertWithIdentityDatabase(entity);

            case StorageMode.Cached:
                return InsertWithIdentityCached(entity);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int InsertWithIdentityInMemory(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var (entityCopy, id) = _entity.PrepareForInsertWithIdentity(entity);
            _entities.Add(entityCopy);
            _entity.CopyIdentityValue(entityCopy, entity);
            return (int)id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int InsertWithIdentityDatabase(T entity)
    {
        using (var db = CreateConnection())
        {
            var id = Convert.ToInt32(db.InsertWithIdentity(entity));

            if (_entity.IdentityManager.IdentityProperty != null)
            {
                _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
            }

            return id;
        }
    }

    private int InsertWithIdentityCached(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var id = Convert.ToInt32(db.InsertWithIdentity(entity));

                if (_entity.IdentityManager.IdentityProperty != null)
                {
                    _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
                }

                if (id > 0)
                {
                    var entityCopy = _entity.CreateDeepCopy(entity);
                    _entities.Add(entityCopy);
                }

                return id;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public long InsertWithInt64Identity(T entity)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return InsertWithInt64IdentityInMemory(entity);

            case StorageMode.Database:
                return InsertWithInt64IdentityDatabase(entity);

            case StorageMode.Cached:
                return InsertWithInt64IdentityCached(entity);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private long InsertWithInt64IdentityInMemory(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var (entityCopy, id) = _entity.PrepareForInsertWithIdentity(entity);
            _entities.Add(entityCopy);
            _entity.CopyIdentityValue(entityCopy, entity);
            return id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private long InsertWithInt64IdentityDatabase(T entity)
    {
        using (var db = CreateConnection())
        {
            var id = Convert.ToInt64(db.InsertWithIdentity(entity));

            if (_entity.IdentityManager.IdentityProperty != null)
            {
                _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
            }

            return id;
        }
    }

    private long InsertWithInt64IdentityCached(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var id = Convert.ToInt64(db.InsertWithIdentity(entity));

                if (_entity.IdentityManager.IdentityProperty != null)
                {
                    _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
                }

                if (id > 0)
                {
                    var entityCopy = _entity.CreateDeepCopy(entity);
                    _entities.Add(entityCopy);
                }

                return id;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int InsertRange(IEnumerable<T> entities)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return InsertRangeInMemory(entities);

            case StorageMode.Database:
                return InsertRangeDatabase(entities);

            case StorageMode.Cached:
                return InsertRangeCached(entities);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int InsertRangeInMemory(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                var entityCopy = _entity.PrepareForInsert(entity);
                _entities.Add(entityCopy);
                _entity.CopyIdentityValue(entityCopy, entity);
                count++;
            }
            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int InsertRangeDatabase(IEnumerable<T> entities)
    {
        using (var db = CreateConnection())
        {
            var count = 0;
            foreach (var entity in entities)
            {
                count += db.Insert(entity);
            }
            return count;
        }
    }

    private int InsertRangeCached(IEnumerable<T> entities)
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
                    var dbResult = db.Insert(entity);
                    if (dbResult > 0)
                    {
                        var entityCopy = _entity.CreateDeepCopy(entity);
                        _entities.Add(entityCopy);
                        count++;
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

    // Read Operations
    public IEnumerable<T> GetAll()
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return GetAllInMemory();

            case StorageMode.Database:
                return GetAllDatabase();

            case StorageMode.Cached:
                return GetAllCached();

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private IEnumerable<T> GetAllInMemory()
    {
        _lock.EnterReadLock();
        try
        {
            return _entities.Select(_entity.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private IEnumerable<T> GetAllDatabase()
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().ToList();
        }
    }

    private IEnumerable<T> GetAllCached()
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            return _entities.Select(_entity.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return FindInMemory(predicate);

            case StorageMode.Database:
                return FindDatabase(predicate);

            case StorageMode.Cached:
                return FindCached(predicate);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private IEnumerable<T> FindInMemory(Expression<Func<T, bool>> predicate)
    {
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            return _entities.Where(compiledPredicate).Select(_entity.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private IEnumerable<T> FindDatabase(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Where(predicate).ToList();
        }
    }

    private IEnumerable<T> FindCached(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            return _entities.Where(compiledPredicate).Select(_entity.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public T FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return FirstOrDefaultInMemory(predicate);

            case StorageMode.Database:
                return FirstOrDefaultDatabase(predicate);

            case StorageMode.Cached:
                return FirstOrDefaultCached(predicate);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private T FirstOrDefaultInMemory(Expression<Func<T, bool>> predicate)
    {
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            var entity = _entities.FirstOrDefault(compiledPredicate);
            return entity != null ? _entity.CreateDeepCopy(entity) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private T FirstOrDefaultDatabase(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().FirstOrDefault(predicate);
        }
    }

    private T FirstOrDefaultCached(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            var entity = _entities.FirstOrDefault(compiledPredicate);
            return entity != null ? _entity.CreateDeepCopy(entity) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return SelectInMemory(selector);

            case StorageMode.Database:
                return SelectDatabase(selector);

            case StorageMode.Cached:
                return SelectCached(selector);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private IEnumerable<TResult> SelectInMemory<TResult>(Expression<Func<T, TResult>> selector)
    {
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

    private IEnumerable<TResult> SelectDatabase<TResult>(Expression<Func<T, TResult>> selector)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Select(selector).ToList();
        }
    }

    private IEnumerable<TResult> SelectCached<TResult>(Expression<Func<T, TResult>> selector)
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return QueryInMemory(query);

            case StorageMode.Database:
                return QueryDatabase(query);

            case StorageMode.Cached:
                return QueryCached(query);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private IEnumerable<TResult> QueryInMemory<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        _lock.EnterReadLock();
        try
        {
            var copiedQueryable = _entities.Select(_entity.CreateDeepCopy).AsQueryable();
            return query(copiedQueryable).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private IEnumerable<TResult> QueryDatabase<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        using (var db = CreateConnection())
        {
            return query(db.GetTable<T>()).ToList();
        }
    }

    private IEnumerable<TResult> QueryCached<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        EnsureInitialized();
        _lock.EnterReadLock();
        try
        {
            var copiedQueryable = _entities.Select(_entity.CreateDeepCopy).AsQueryable();
            return query(copiedQueryable).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Update Operations
    public int Update(T entity)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return UpdateInMemory(entity);

            case StorageMode.Database:
                return UpdateDatabase(entity);

            case StorageMode.Cached:
                return UpdateCached(entity);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int UpdateInMemory(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_entity.PrimaryKeyProperties.Count == 0)
                return 0;

            var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
            if (existingEntity == null)
                return 0;

            var entityCopy = _entity.CreateDeepCopy(entity);
            _entity.UpdateEntityValues(existingEntity, entityCopy);

            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int UpdateDatabase(T entity)
    {
        using (var db = CreateConnection())
        {
            return db.Update(entity);
        }
    }

    private int UpdateCached(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var dbResult = db.Update(entity);

                if (dbResult > 0 && _entity.PrimaryKeyProperties.Count > 0)
                {
                    var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
                    if (existingEntity != null)
                    {
                        var entityCopy = _entity.CreateDeepCopy(entity);
                        _entity.UpdateEntityValues(existingEntity, entityCopy);
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return UpdateRangeInMemory(entities);

            case StorageMode.Database:
                return UpdateRangeDatabase(entities);

            case StorageMode.Cached:
                return UpdateRangeCached(entities);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int UpdateRangeInMemory(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (_entity.PrimaryKeyProperties.Count == 0)
                    continue;

                var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
                if (existingEntity == null)
                    continue;

                var entityCopy = _entity.CreateDeepCopy(entity);
                _entity.UpdateEntityValues(existingEntity, entityCopy);
                count++;
            }
            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int UpdateRangeDatabase(IEnumerable<T> entities)
    {
        using (var db = CreateConnection())
        {
            var count = 0;
            foreach (var entity in entities)
            {
                count += db.Update(entity);
            }
            return count;
        }
    }

    private int UpdateRangeCached(IEnumerable<T> entities)
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

                    if (dbResult > 0 && _entity.PrimaryKeyProperties.Count > 0)
                    {
                        var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
                        if (existingEntity != null)
                        {
                            var entityCopy = _entity.CreateDeepCopy(entity);
                            _entity.UpdateEntityValues(existingEntity, entityCopy);
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

    // Delete Operations
    public int Delete(T entity)
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return DeleteInMemory(entity);

            case StorageMode.Database:
                return DeleteDatabase(entity);

            case StorageMode.Cached:
                return DeleteCached(entity);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int DeleteInMemory(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_entity.PrimaryKeyProperties.Count == 0)
                return 0;

            var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
            if (existingEntity == null)
                return 0;

            _entities.Remove(existingEntity);
            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int DeleteDatabase(T entity)
    {
        using (var db = CreateConnection())
        {
            return db.Delete(entity);
        }
    }

    private int DeleteCached(T entity)
    {
        EnsureInitialized();
        _lock.EnterWriteLock();
        try
        {
            using (var db = CreateConnection())
            {
                var dbResult = db.Delete(entity);

                if (dbResult > 0 && _entity.PrimaryKeyProperties.Count > 0)
                {
                    var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return DeleteRangeInMemory(entities);

            case StorageMode.Database:
                return DeleteRangeDatabase(entities);

            case StorageMode.Cached:
                return DeleteRangeCached(entities);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int DeleteRangeInMemory(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            var entitiesToDelete = entities.ToList();
            foreach (var entity in entitiesToDelete)
            {
                if (_entity.PrimaryKeyProperties.Count == 0)
                    continue;

                var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
                if (existingEntity == null)
                    continue;

                _entities.Remove(existingEntity);
                count++;
            }
            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int DeleteRangeDatabase(IEnumerable<T> entities)
    {
        using (var db = CreateConnection())
        {
            var count = 0;
            foreach (var entity in entities)
            {
                count += db.Delete(entity);
            }
            return count;
        }
    }

    private int DeleteRangeCached(IEnumerable<T> entities)
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

                    if (dbResult > 0 && _entity.PrimaryKeyProperties.Count > 0)
                    {
                        var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return DeleteManyInMemory(predicate);

            case StorageMode.Database:
                return DeleteManyDatabase(predicate);

            case StorageMode.Cached:
                return DeleteManyCached(predicate);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int DeleteManyInMemory(Expression<Func<T, bool>> predicate)
    {
        _lock.EnterWriteLock();
        try
        {
            var compiledPredicate = predicate.Compile();
            var entitiesToDelete = _entities.Where(compiledPredicate).ToList();
            foreach (var entity in entitiesToDelete)
            {
                _entities.Remove(entity);
            }
            return entitiesToDelete.Count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private int DeleteManyDatabase(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Where(predicate).Delete();
        }
    }

    private int DeleteManyCached(Expression<Func<T, bool>> predicate)
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

    // Utility Operations
    public int Count()
    {
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return CountInMemory();

            case StorageMode.Database:
                return CountDatabase();

            case StorageMode.Cached:
                return CountCached();

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int CountInMemory()
    {
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

    private int CountDatabase()
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Count();
        }
    }

    private int CountCached()
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return CountPredicateInMemory(predicate);

            case StorageMode.Database:
                return CountPredicateDatabase(predicate);

            case StorageMode.Cached:
                return CountPredicateCached(predicate);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private int CountPredicateInMemory(Expression<Func<T, bool>> predicate)
    {
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

    private int CountPredicateDatabase(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Count(predicate);
        }
    }

    private int CountPredicateCached(Expression<Func<T, bool>> predicate)
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
        switch (_options.Mode)
        {
            case StorageMode.InMemory:
                return ExistsInMemory(predicate);

            case StorageMode.Database:
                return ExistsDatabase(predicate);

            case StorageMode.Cached:
                return ExistsCached(predicate);

            default:
                throw new InvalidOperationException($"Unknown storage mode: {_options.Mode}");
        }
    }

    private bool ExistsInMemory(Expression<Func<T, bool>> predicate)
    {
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

    private bool ExistsDatabase(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Any(predicate);
        }
    }

    private bool ExistsCached(Expression<Func<T, bool>> predicate)
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

    public void Dispose()
    {
        _lock?.Dispose();
    }
}
