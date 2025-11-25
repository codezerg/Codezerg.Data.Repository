using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Codezerg.Data.Repository.Storage;

/// <summary>
/// Internal interface defining storage operations for different repository modes
/// </summary>
internal interface IStorageStrategy<T> : IDisposable where T : class, new()
{
    // Create Operations
    T Insert(T entity);
    IEnumerable<T> InsertRange(IEnumerable<T> entities);

    // Read Operations
    IEnumerable<T> GetAll();
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    T FirstOrDefault(Expression<Func<T, bool>> predicate);
    IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query);

    // Update Operations
    int Update(T entity);
    int UpdateRange(IEnumerable<T> entities);

    // Delete Operations
    int Delete(T entity);
    int DeleteRange(IEnumerable<T> entities);
    int DeleteMany(Expression<Func<T, bool>> predicate);

    // Utility Operations
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    bool Exists(Expression<Func<T, bool>> predicate);

    // Mode-specific Operations
    void Clear();
    void Refresh();
}
