using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Codezerg.Data.Repository
{
    /// <summary>
    /// Repository that caches data in memory but persists to SQLite using TableRepository
    /// </summary>
    public class CachedRepository<T> : IRepository<T>, IDisposable where T : class, new()
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
        public int Insert(T entity)
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

        public int InsertWithIdentity(T entity)
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

        public long InsertWithInt64Identity(T entity)
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

        public int InsertRange(IEnumerable<T> entities)
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
        public IEnumerable<T> GetAll()
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

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
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

        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
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

        public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
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

        public IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
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
        public int Update(T entity)
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

        public int UpdateRange(IEnumerable<T> entities)
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
        public int Delete(T entity)
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

        public int DeleteRange(IEnumerable<T> entities)
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
        public int Count()
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

        public int Count(Expression<Func<T, bool>> predicate)
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

        public bool Exists(Expression<Func<T, bool>> predicate)
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

        public int DeleteMany(Expression<Func<T, bool>> predicate)
        {
            EnsureInitialized();

            try
            {
                _lock.EnterWriteLock();

                // Delete from database first using the table repository
                var count = _tableRepository.DeleteMany(predicate);

                if (count > 0)
                {
                    // If successful, delete from memory by finding matching entities first
                    var matchingEntities = _memoryRepository.Find(predicate).ToList();
                    _memoryRepository.DeleteRange(matchingEntities);
                }

                return count;
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
}