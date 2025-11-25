using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Codezerg.Data.Repository.Storage;

/// <summary>
/// Thread-safe in-memory storage implementation
/// </summary>
internal sealed class InMemoryStorage<T> : IStorageStrategy<T> where T : class, new()
{
    private readonly List<T> _entities;
    private readonly ReaderWriterLockSlim _lock;
    private readonly EntityOperations<T> _entityOps;

    public InMemoryStorage(EntityOperations<T> entityOps)
    {
        _entityOps = entityOps ?? throw new ArgumentNullException(nameof(entityOps));
        _entities = new List<T>();
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    }

    public T Insert(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var entityCopy = _entityOps.PrepareForInsert(entity);
            _entities.Add(entityCopy);
            _entityOps.CopyIdentityValue(entityCopy, entity);
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<T> InsertRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var entitiesList = entities.ToList();
            foreach (var entity in entitiesList)
            {
                var entityCopy = _entityOps.PrepareForInsert(entity);
                _entities.Add(entityCopy);
                _entityOps.CopyIdentityValue(entityCopy, entity);
            }
            return entitiesList;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerable<T> GetAll()
    {
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
        _lock.EnterWriteLock();
        try
        {
            if (_entityOps.PrimaryKeyProperties.Count == 0)
                return 0;

            var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
            if (existingEntity == null)
                return 0;

            var entityCopy = _entityOps.CreateDeepCopy(entity);
            _entityOps.UpdateEntityValues(existingEntity, entityCopy);

            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int UpdateRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (_entityOps.PrimaryKeyProperties.Count == 0)
                    continue;

                var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
                if (existingEntity == null)
                    continue;

                var entityCopy = _entityOps.CreateDeepCopy(entity);
                _entityOps.UpdateEntityValues(existingEntity, entityCopy);
                count++;
            }
            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public int Delete(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_entityOps.PrimaryKeyProperties.Count == 0)
                return 0;

            var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
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

    public int DeleteRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            var entitiesToDelete = entities.ToList();
            foreach (var entity in entitiesToDelete)
            {
                if (_entityOps.PrimaryKeyProperties.Count == 0)
                    continue;

                var existingEntity = _entityOps.FindEntityByPrimaryKeys(_entities, entity);
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

    public int DeleteMany(Expression<Func<T, bool>> predicate)
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

    public int Count()
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

    public int Count(Expression<Func<T, bool>> predicate)
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

    public bool Exists(Expression<Func<T, bool>> predicate)
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
        throw new InvalidOperationException("Refresh is only supported in Cached mode");
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }
}
