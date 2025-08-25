using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Codezerg.Data.Repository.Core;
using Microsoft.Extensions.Logging;

namespace Codezerg.Data.Repository.Infrastructure.Diagnostics;

/// <summary>
/// Decorator that adds logging and performance metrics to repository operations
/// </summary>
public class RepositoryLoggingDecorator<T> : RepositoryBase<T> where T : class, new()
{
    private readonly IRepository<T> _repository;
    private readonly ILogger<RepositoryLoggingDecorator<T>>? _logger;
    private readonly bool _logPerformance;
    
    public RepositoryLoggingDecorator(IRepository<T> repository, ILogger<RepositoryLoggingDecorator<T>>? logger = null, bool logPerformance = true)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
        _logPerformance = logPerformance;
    }
    
    public override int Insert(T entity)
    {
        return ExecuteWithLogging(
            () => _repository.Insert(entity),
            "Insert",
            $"Inserting entity of type {typeof(T).Name}"
        );
    }
    
    public override int InsertWithIdentity(T entity)
    {
        return ExecuteWithLogging(
            () => _repository.InsertWithIdentity(entity),
            "InsertWithIdentity",
            $"Inserting entity with identity of type {typeof(T).Name}"
        );
    }
    
    public override long InsertWithInt64Identity(T entity)
    {
        return ExecuteWithLogging(
            () => _repository.InsertWithInt64Identity(entity),
            "InsertWithInt64Identity",
            $"Inserting entity with Int64 identity of type {typeof(T).Name}"
        );
    }
    
    public override int InsertRange(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        return ExecuteWithLogging(
            () => _repository.InsertRange(entityList),
            "InsertRange",
            $"Inserting {entityList.Count} entities of type {typeof(T).Name}"
        );
    }
    
    public override IEnumerable<T> GetAll()
    {
        return ExecuteWithLogging(
            () => _repository.GetAll(),
            "GetAll",
            $"Getting all entities of type {typeof(T).Name}"
        );
    }
    
    public override IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
    {
        return ExecuteWithLogging(
            () => _repository.Find(predicate),
            "Find",
            $"Finding entities of type {typeof(T).Name}"
        );
    }
    
    public override T? FirstOrDefault(Expression<Func<T, bool>> predicate)
    {
        return ExecuteWithLogging(
            () => _repository.FirstOrDefault(predicate),
            "FirstOrDefault",
            $"Finding first entity of type {typeof(T).Name}"
        );
    }
    
    public override IEnumerable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        return ExecuteWithLogging(
            () => _repository.Select(selector),
            "Select",
            $"Selecting from entities of type {typeof(T).Name}"
        );
    }
    
    public override IEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IEnumerable<TResult>> query)
    {
        return ExecuteWithLogging(
            () => _repository.Query(query),
            "Query",
            $"Querying entities of type {typeof(T).Name}"
        );
    }
    
    public override int Update(T entity)
    {
        return ExecuteWithLogging(
            () => _repository.Update(entity),
            "Update",
            $"Updating entity of type {typeof(T).Name}"
        );
    }
    
    public override int UpdateRange(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        return ExecuteWithLogging(
            () => _repository.UpdateRange(entityList),
            "UpdateRange",
            $"Updating {entityList.Count} entities of type {typeof(T).Name}"
        );
    }
    
    public override int Delete(T entity)
    {
        return ExecuteWithLogging(
            () => _repository.Delete(entity),
            "Delete",
            $"Deleting entity of type {typeof(T).Name}"
        );
    }
    
    public override int DeleteRange(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        return ExecuteWithLogging(
            () => _repository.DeleteRange(entityList),
            "DeleteRange",
            $"Deleting {entityList.Count} entities of type {typeof(T).Name}"
        );
    }
    
    public override int Count()
    {
        return ExecuteWithLogging(
            () => _repository.Count(),
            "Count",
            $"Counting entities of type {typeof(T).Name}"
        );
    }
    
    public override int Count(Expression<Func<T, bool>> predicate)
    {
        return ExecuteWithLogging(
            () => _repository.Count(predicate),
            "Count",
            $"Counting entities of type {typeof(T).Name} with predicate"
        );
    }
    
    public override bool Exists(Expression<Func<T, bool>> predicate)
    {
        return ExecuteWithLogging(
            () => _repository.Exists(predicate),
            "Exists",
            $"Checking existence of entity of type {typeof(T).Name}"
        );
    }
    
    private TResult ExecuteWithLogging<TResult>(Func<TResult> operation, string operationName, string message)
    {
        if (_logger == null)
            return operation();
        
        _logger.LogDebug($"Starting {operationName}: {message}");
        
        var stopwatch = _logPerformance ? Stopwatch.StartNew() : null;
        
        try
        {
            var result = operation();
            
            if (_logPerformance && stopwatch != null)
            {
                stopwatch.Stop();
                _logger.LogInformation($"Completed {operationName} in {stopwatch.ElapsedMilliseconds}ms: {message}");
            }
            else
            {
                _logger.LogDebug($"Completed {operationName}: {message}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in {operationName}: {message}");
            throw;
        }
    }
}