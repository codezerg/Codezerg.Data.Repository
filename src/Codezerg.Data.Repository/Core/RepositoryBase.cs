using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Codezerg.Data.Repository.Core;

public abstract class RepositoryBase<T> : IRepository<T> where T : class, new()
{
    public abstract int Insert(T entity);
    public abstract int InsertWithIdentity(T entity);
    public abstract long InsertWithInt64Identity(T entity);
    public abstract int InsertRange(IEnumerable<T> entities);
    
    public abstract IEnumerable<T> GetAll();
    public abstract IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    public abstract T? FirstOrDefault(Expression<Func<T, bool>> predicate);
    public abstract IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    public abstract IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query);
    
    public abstract int Update(T entity);
    public abstract int UpdateRange(IEnumerable<T> entities);
    
    public abstract int Delete(T entity);
    public abstract int DeleteRange(IEnumerable<T> entities);
    
    public abstract int Count();
    public abstract int Count(Expression<Func<T, bool>> predicate);
    public abstract bool Exists(Expression<Func<T, bool>> predicate);
    
    public virtual int DeleteMany(Expression<Func<T, bool>> predicate)
    {
        var items = Find(predicate).ToList();
        return DeleteRange(items);
    }
}