using System;
using System.IO;
using LinqToDB;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Implementations;

namespace Codezerg.Data.Repository.Infrastructure;

public class RepositoryFactory : IRepositoryFactory
{
    private readonly RepositoryOptions _options;
    
    public RepositoryFactory() : this(new RepositoryOptions())
    {
    }
    
    public RepositoryFactory(RepositoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public IRepository<T> GetRepository<T>() where T : class, new()
    {
        // Ensure data folder exists
        if (!Directory.Exists(_options.DataPath))
        {
            Directory.CreateDirectory(_options.DataPath);
        }
        
        // Get database name
        var databaseName = _options.DatabaseNameResolver != null 
            ? _options.DatabaseNameResolver(typeof(T))
            : EntityMapping<T>.GetDatabaseName();
        
        // Build connection string
        var connectionString = _options.ConnectionStringTemplate
            .Replace("{DataPath}", _options.DataPath)
            .Replace("{DatabaseName}", databaseName);
        
        // Create repository based on options
        if (_options.UseCachedRepository)
        {
            return new CachedRepository<T>(_options.ProviderName, connectionString);
        }
        else
        {
            return new DatabaseRepository<T>(_options.ProviderName, connectionString);
        }
    }
}