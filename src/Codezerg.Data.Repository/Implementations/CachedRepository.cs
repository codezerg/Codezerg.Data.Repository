using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Infrastructure;

namespace Codezerg.Data.Repository.Implementations;

/// <summary>
/// Repository that caches data in memory but persists to SQLite using TableRepository
/// </summary>
public class CachedRepository<T> : RepositoryBase<T>, IDisposable where T : class, new()
{
    private readonly InMemoryRepository<T> _memoryRepository;
    private readonly DatabaseRepository<T> _tableRepository;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private bool _isInitialized = false;

    public CachedRepository(string providerName, string connectionString)
    {
        _memoryRepository = new InMemoryRepository<T>();
        _tableRepository = new DatabaseRepository<T>(providerName, connectionString);

        LoadDataFromDatabase();
    }

    private void LoadDataFromDatabase()
    {
        try
        {
            _lock.EnterWriteLock();

            // Clear any existing data
            _memoryRepository.Clear();

            // Load all data from the table repository
            var allEntities = _tableRepository.GetAll().ToList();

            // Add to in-memory repository
            _memoryRepository.InsertRange(allEntities);

            _isInitialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Create Operations
    public override int Insert(T entity)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            // Insert to database first using the table repository
            var dbResult = _tableRepository.Insert(entity);

            if (dbResult > 0)
            {
                // If successful, insert to memory
                _memoryRepository.Insert(entity);
            }

            return dbResult;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int InsertWithIdentity(T entity)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            // Insert to database first to get identity using the table repository
            var id = _tableRepository.InsertWithIdentity(entity);

            if (id > 0)
            {
                // If successful, insert to memory
                _memoryRepository.Insert(entity);
            }

            return id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override long InsertWithInt64Identity(T entity)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            // Insert to database first to get identity using the table repository
            var id = _tableRepository.InsertWithInt64Identity(entity);

            if (id > 0)
            {
                // If successful, insert to memory
                _memoryRepository.Insert(entity);
            }

            return id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int InsertRange(IEnumerable<T> entities)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            var entitiesList = entities.ToList();

            // Insert to database first using the table repository
            var count = _tableRepository.InsertRange(entitiesList);

            if (count > 0)
            {
                // If successful, insert to memory
                _memoryRepository.InsertRange(entitiesList);
            }

            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Read Operations
    public override IEnumerable<T> GetAll()
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.GetAll();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Find(predicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override T? FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.FirstOrDefault(predicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Select(selector);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Query(query);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Update Operations
    public override int Update(T entity)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            // Update database first using the table repository
            var dbResult = _tableRepository.Update(entity);

            if (dbResult > 0)
            {
                // If successful, update memory
                _memoryRepository.Update(entity);
            }

            return dbResult;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int UpdateRange(IEnumerable<T> entities)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            var entitiesList = entities.ToList();

            // Update database first using the table repository
            var count = _tableRepository.UpdateRange(entitiesList);

            if (count > 0)
            {
                // If successful, update memory
                _memoryRepository.UpdateRange(entitiesList);
            }

            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Delete Operations
    public override int Delete(T entity)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            // Delete from database first using the table repository
            var dbResult = _tableRepository.Delete(entity);

            if (dbResult > 0)
            {
                // If successful, delete from memory
                _memoryRepository.Delete(entity);
            }

            return dbResult;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int DeleteRange(IEnumerable<T> entities)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterWriteLock();

            var entitiesList = entities.ToList();

            // Delete from database first using the table repository
            var count = _tableRepository.DeleteRange(entitiesList);

            if (count > 0)
            {
                // If successful, delete from memory
                _memoryRepository.DeleteRange(entitiesList);
            }

            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Additional Operations
    public override int Count()
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Count();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override int Count(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Count(predicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override bool Exists(Expression<Func<T, bool>> predicate)
    {
        EnsureInitialized();

        try
        {
            _lock.EnterReadLock();
            return _memoryRepository.Exists(predicate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Helper methods
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            LoadDataFromDatabase();
        }
    }

    // Method to force reload from database
    public void Refresh()
    {
        LoadDataFromDatabase();
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }
}