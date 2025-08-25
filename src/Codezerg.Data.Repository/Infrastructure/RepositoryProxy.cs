using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Codezerg.Data.Repository.Core;

namespace Codezerg.Data.Repository.Infrastructure;

public class RepositoryProxy<T> : IRepository<T> where T : class, new()
{
    private readonly IRepository<T> _repository;

    public RepositoryProxy(IRepositoryFactory repositoryFactory)
    {
        _repository = repositoryFactory.GetRepository<T>();
    }

    public int Count() => _repository.Count();
    public int Count(Expression<Func<T, bool>> predicate) => _repository.Count(predicate);
    public bool Exists(Expression<Func<T, bool>> predicate) => _repository.Exists(predicate);

    public int Delete(T entity) => _repository.Delete(entity);
    public int DeleteRange(IEnumerable<T> entities) => _repository.DeleteRange(entities);

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => _repository.Find(predicate);
    public T? FirstOrDefault(Expression<Func<T, bool>> predicate) => _repository.FirstOrDefault(predicate);
    public IEnumerable<T> GetAll() => _repository.GetAll();

    public int Insert(T entity) => _repository.Insert(entity);
    public int InsertRange(IEnumerable<T> entities) => _repository.InsertRange(entities);
    public int InsertWithIdentity(T entity) => _repository.InsertWithIdentity(entity);
    public long InsertWithInt64Identity(T entity) => _repository.InsertWithInt64Identity(entity);

    public IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query) =>
        _repository.Query(query);

    public IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) =>
        _repository.Select(selector);

    public int Update(T entity) => _repository.Update(entity);
    public int UpdateRange(IEnumerable<T> entities) => _repository.UpdateRange(entities);

    public int DeleteMany(Expression<Func<T, bool>> predicate) => _repository.DeleteMany(predicate);
}