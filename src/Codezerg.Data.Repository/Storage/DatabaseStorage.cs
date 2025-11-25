using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Codezerg.Data.Repository.Migration;

namespace Codezerg.Data.Repository.Storage;

/// <summary>
/// Direct database storage implementation using linq2db
/// </summary>
internal sealed class DatabaseStorage<T> : IStorageStrategy<T> where T : class, new()
{
    private readonly string _providerName;
    private readonly string _connectionString;
    private readonly MappingSchema _mappingSchema;
    private readonly EntityOperations<T> _entityOps;

    public DatabaseStorage(string providerName, string connectionString, EntityOperations<T> entityOps)
    {
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _entityOps = entityOps ?? throw new ArgumentNullException(nameof(entityOps));
        _mappingSchema = EntityMapping<T>.GetMappingSchema();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (var db = CreateConnection())
        {
            SchemaManager<T>.EnsureSchema(db, _mappingSchema);
        }
    }

    internal DataConnection CreateConnection()
    {
        var db = new DataConnection(_providerName, _connectionString, _mappingSchema);
        if (db.DataProvider.Name.ToLowerInvariant().Contains("sqlite"))
        {
            db.Execute("pragma journal_mode = WAL;");
        }
        return db;
    }

    public T Insert(T entity)
    {
        using (var db = CreateConnection())
        {
            if (_entityOps.IdentityManager.IdentityProperty != null)
            {
                var id = db.InsertWithIdentity(entity);
                var propertyType = _entityOps.IdentityManager.IdentityProperty.PropertyType;
                _entityOps.IdentityManager.IdentityProperty.SetValue(entity,
                    Convert.ChangeType(id, propertyType));
            }
            else
            {
                db.Insert(entity);
            }

            return entity;
        }
    }

    public IEnumerable<T> InsertRange(IEnumerable<T> entities)
    {
        using (var db = CreateConnection())
        {
            var entitiesList = entities.ToList();
            foreach (var entity in entitiesList)
            {
                if (_entityOps.IdentityManager.IdentityProperty != null)
                {
                    var id = db.InsertWithIdentity(entity);
                    var propertyType = _entityOps.IdentityManager.IdentityProperty.PropertyType;
                    _entityOps.IdentityManager.IdentityProperty.SetValue(entity,
                        Convert.ChangeType(id, propertyType));
                }
                else
                {
                    db.Insert(entity);
                }
            }
            return entitiesList;
        }
    }

    public IEnumerable<T> GetAll()
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().ToList();
        }
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Where(predicate).ToList();
        }
    }

    public T FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().FirstOrDefault(predicate);
        }
    }

    public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Select(selector).ToList();
        }
    }

    public IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        using (var db = CreateConnection())
        {
            return query(db.GetTable<T>()).ToList();
        }
    }

    public int Update(T entity)
    {
        using (var db = CreateConnection())
        {
            return db.Update(entity);
        }
    }

    public int UpdateRange(IEnumerable<T> entities)
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

    public int Delete(T entity)
    {
        using (var db = CreateConnection())
        {
            return db.Delete(entity);
        }
    }

    public int DeleteRange(IEnumerable<T> entities)
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

    public int DeleteMany(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Where(predicate).Delete();
        }
    }

    public int Count()
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Count();
        }
    }

    public int Count(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Count(predicate);
        }
    }

    public bool Exists(Expression<Func<T, bool>> predicate)
    {
        using (var db = CreateConnection())
        {
            return db.GetTable<T>().Any(predicate);
        }
    }

    public void Clear()
    {
        throw new InvalidOperationException("Clear is not supported in Database mode. Use DeleteMany or database operations.");
    }

    public void Refresh()
    {
        throw new InvalidOperationException("Refresh is only supported in Cached mode");
    }

    public void Dispose()
    {
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
