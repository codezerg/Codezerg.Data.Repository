using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Infrastructure;

namespace Codezerg.Data.Repository.Implementations;

public class InMemoryRepository<T> : RepositoryBase<T>, IDisposable where T : class, new()
{
    private readonly EntityOperations<T> _entity;
    private readonly List<T> _entities = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);


    public InMemoryRepository()
    {
        _entity = new EntityOperations<T>();
    }


    // Create Operations
    public override int Insert(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            // Create a deep copy to avoid external modifications affecting our stored data
            var entityCopy = _entity.PrepareForInsert(entity);
            _entities.Add(entityCopy);

            // Copy any generated identity back to the original entity
            _entity.CopyIdentityValue(entityCopy, entity);

            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int InsertWithIdentity(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var (entityCopy, id) = _entity.PrepareForInsertWithIdentity(entity);
            _entities.Add(entityCopy);

            // Copy the generated identity back to the original entity
            _entity.CopyIdentityValue(entityCopy, entity);

            return (int)id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override long InsertWithInt64Identity(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            var (entityCopy, id) = _entity.PrepareForInsertWithIdentity(entity);
            _entities.Add(entityCopy);

            // Copy the generated identity back to the original entity
            _entity.CopyIdentityValue(entityCopy, entity);

            return id;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int InsertRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                // Inline the insert logic to avoid recursive locks
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

    // Read Operations
    public override IEnumerable<T> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            // Return copies of all entities to prevent external modifications
            return _entities.Select(_entity.CreateDeepCopy).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public override IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
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

    public override T? FirstOrDefault(Expression<Func<T, bool>> predicate)
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

    public override IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
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

    public override IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        _lock.EnterReadLock();
        try
        {
            // Create a queryable of copies to prevent modifications to internal data
            var copiedQueryable = _entities.Select(_entity.CreateDeepCopy).AsQueryable();
            return query(copiedQueryable).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Update Operations
    public override int Update(T entity)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_entity.PrimaryKeyProperties.Count == 0)
                return 0;

            var existingEntity = _entity.FindEntityByPrimaryKeys(_entities, entity);
            if (existingEntity == null)
                return 0;

            // Create a copy of the entity to update
            var entityCopy = _entity.CreateDeepCopy(entity);

            // Update properties of the existing entity
            _entity.UpdateEntityValues(existingEntity, entityCopy);

            return 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override int UpdateRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                // Inline update logic to avoid recursive locks
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

    // Delete Operations
    public override int Delete(T entity)
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

    public override int DeleteRange(IEnumerable<T> entities)
    {
        _lock.EnterWriteLock();
        try
        {
            var count = 0;
            var entitiesToDelete = entities.ToList();
            foreach (var entity in entitiesToDelete)
            {
                // Inline delete logic to avoid recursive locks
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

    public override int Count()
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

    public override int Count(Expression<Func<T, bool>> predicate)
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

    public override bool Exists(Expression<Func<T, bool>> predicate)
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

    // For testing purposes
    public void Clear()
    {
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

    public void Dispose()
    {
        _lock?.Dispose();
    }
}