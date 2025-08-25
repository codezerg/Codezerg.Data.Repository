using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Infrastructure;

namespace Codezerg.Data.Repository.Implementations;

public class DatabaseRepository<T> : RepositoryBase<T> where T : class, new()
{
    private readonly string _providerName;
    private readonly string _connectionString;
    private readonly MappingSchema _mappingSchema;
    private readonly EntityOperations<T> _entity;

    public DatabaseRepository(string providerName, string connectionString) : base()
    {
        _entity = new EntityOperations<T>();
        _providerName = providerName;
        _connectionString = connectionString;
        _mappingSchema = EntityMapping<T>.GetMappingSchema();

        bool tableExists = false;

        try
        {
            using var db = CreateConnection();
            db.GetTable<T>().Any();
            tableExists = true;
        }
        catch
        {
            tableExists = false;
        }

        if (!tableExists)
        {
            using var db = CreateConnection();
            // Let linq2db handle table creation with its own mapping
            db.CreateTable<T>();
        }
    }

    private DataConnection CreateConnection()
    {
        var db = new DataConnection(_providerName, _connectionString, _mappingSchema);
        if (db.DataProvider.Name.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            db.Execute("pragma journal_mode = WAL;");
            //db.Execute("pragma page_size = 4096;");
            //db.Execute("pragma synchronous = normal;");
            //db.Execute("pragma temp_store = memory;");
        }
        return db;
    }

    // Create Operations
    public override int Insert(T entity)
    {
        using var db = CreateConnection();
        return db.Insert(entity);
    }

    public override int InsertWithIdentity(T entity)
    {
        using var db = CreateConnection();
        var id = Convert.ToInt32(db.InsertWithIdentity(entity));

        // If we have an identity property, copy the generated value back to the original entity
        if (_entity.IdentityManager.IdentityProperty != null)
        {
            _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
        }

        return id;
    }

    public override long InsertWithInt64Identity(T entity)
    {
        using var db = CreateConnection();
        var id = Convert.ToInt64(db.InsertWithIdentity(entity));

        // If we have an identity property, copy the generated value back to the original entity
        if (_entity.IdentityManager.IdentityProperty != null)
        {
            _entity.IdentityManager.IdentityProperty.SetValue(entity, id);
        }

        return id;
    }

    public override int InsertRange(IEnumerable<T> entities)
    {
        using var db = CreateConnection();
        var count = 0;
        foreach (var entity in entities)
        {
            count += db.Insert(entity);
        }
        return count;
    }

    // Read Operations
    public override IEnumerable<T> GetAll()
    {
        using var db = CreateConnection();
        return db.GetTable<T>().ToList();
    }

    public override IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        using var db = CreateConnection();
        return db.GetTable<T>().Where(predicate).ToList();
    }

    public override T? FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        using var db = CreateConnection();
        return db.GetTable<T>().FirstOrDefault(predicate);
    }

    // Override base implementations for better performance with direct database access
    public override IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        using var db = CreateConnection();
        return db.GetTable<T>().Select(selector).ToList();
    }

    public override IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        using var db = CreateConnection();
        return query(db.GetTable<T>()).ToList();
    }

    // Update Operations
    public override int Update(T entity)
    {
        using var db = CreateConnection();
        return db.Update(entity);
    }

    public override int UpdateRange(IEnumerable<T> entities)
    {
        using var db = CreateConnection();
        var count = 0;
        foreach (var entity in entities)
        {
            count += db.Update(entity);
        }
        return count;
    }

    // Delete Operations
    public override int Delete(T entity)
    {
        using var db = CreateConnection();
        return db.Delete(entity);
    }

    public override int DeleteRange(IEnumerable<T> entities)
    {
        using var db = CreateConnection();
        var count = 0;
        foreach (var entity in entities)
        {
            count += db.Delete(entity);
        }
        return count;
    }

    public override int Count()
    {
        using var db = CreateConnection();
        return db.GetTable<T>().Count();
    }

    public override int Count(Expression<Func<T, bool>> predicate)
    {
        using var db = CreateConnection();
        return db.GetTable<T>().Count(predicate);
    }

    public override bool Exists(Expression<Func<T, bool>> predicate)
    {
        using var db = CreateConnection();
        return db.GetTable<T>().Any(predicate);
    }

}