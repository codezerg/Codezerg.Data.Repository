using System;
using System.Collections.Generic;
using System.Linq;
using Codezerg.Data.Repository.Core.Attributes;
using Codezerg.Data.Repository.Core.Configuration;

namespace Codezerg.Data.Repository.Core.Overrides;

/// <summary>
/// Fluent builder for repository configuration overrides
/// </summary>
public class FluentOverrideBuilder
{
    private readonly IRepositoryOverrideManager _overrideManager;
    private readonly List<OverrideEntry> _entries = new();
    
    public FluentOverrideBuilder(IRepositoryOverrideManager overrideManager)
    {
        _overrideManager = overrideManager ?? throw new ArgumentNullException(nameof(overrideManager));
    }
    
    /// <summary>
    /// Start configuring overrides for a specific entity type
    /// </summary>
    public EntityOverrideBuilder<T> ForEntity<T>() where T : class, new()
    {
        return new EntityOverrideBuilder<T>(this);
    }
    
    /// <summary>
    /// Apply all configured overrides
    /// </summary>
    public void Apply()
    {
        foreach (var entry in _entries)
        {
            if (entry.ShouldApply())
            {
                _overrideManager.OverrideFor(entry.EntityType, entry.Configure);
            }
        }
    }
    
    internal void AddEntry(OverrideEntry entry)
    {
        _entries.Add(entry);
    }
    
    internal class OverrideEntry
    {
        public Type EntityType { get; set; } = null!;
        public Action<RepositoryConfiguration> Configure { get; set; } = null!;
        public List<Func<bool>> Conditions { get; set; } = new();
        public List<string> Environments { get; set; } = new();
        
        public bool ShouldApply()
        {
            // Check environment conditions
            if (Environments.Any())
            {
                var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
                                "Production";
                
                if (!Environments.Contains(currentEnv, StringComparer.OrdinalIgnoreCase))
                    return false;
            }
            
            // Check custom conditions
            return Conditions.All(condition => condition());
        }
    }
}

/// <summary>
/// Builder for entity-specific overrides
/// </summary>
public class EntityOverrideBuilder<T> where T : class, new()
{
    private readonly FluentOverrideBuilder _parent;
    private readonly FluentOverrideBuilder.OverrideEntry _entry;
    
    internal EntityOverrideBuilder(FluentOverrideBuilder parent)
    {
        _parent = parent;
        _entry = new FluentOverrideBuilder.OverrideEntry
        {
            EntityType = typeof(T),
            Configure = _ => { } // Default no-op
        };
    }
    
    /// <summary>
    /// Configure to use in-memory repository
    /// </summary>
    public EntityOverrideBuilder<T> UseInMemory(Action<InMemoryOptions>? configure = null)
    {
        _entry.Configure = config =>
        {
            config.Strategy = RepositoryStrategy.InMemory;
            configure?.Invoke(new InMemoryOptions(config));
        };
        return this;
    }
    
    /// <summary>
    /// Configure to use database repository
    /// </summary>
    public EntityOverrideBuilder<T> UseDatabase(Action<DatabaseOptions>? configure = null)
    {
        _entry.Configure = config =>
        {
            config.Strategy = RepositoryStrategy.Database;
            configure?.Invoke(new DatabaseOptions(config));
        };
        return this;
    }
    
    /// <summary>
    /// Configure to use cached repository
    /// </summary>
    public EntityOverrideBuilder<T> UseCached(Action<CachedOptions>? configure = null)
    {
        _entry.Configure = config =>
        {
            config.Strategy = RepositoryStrategy.Cached;
            configure?.Invoke(new CachedOptions(config));
        };
        return this;
    }
    
    /// <summary>
    /// Apply override only when condition is true
    /// </summary>
    public EntityOverrideBuilder<T> When(Func<bool> condition)
    {
        _entry.Conditions.Add(condition);
        return this;
    }
    
    /// <summary>
    /// Apply override only in specific environments
    /// </summary>
    public EntityOverrideBuilder<T> InEnvironment(params string[] environments)
    {
        _entry.Environments.AddRange(environments);
        return this;
    }
    
    /// <summary>
    /// Configure custom settings
    /// </summary>
    public EntityOverrideBuilder<T> Configure(Action<RepositoryConfiguration> configure)
    {
        var previousConfigure = _entry.Configure;
        _entry.Configure = config =>
        {
            previousConfigure(config);
            configure(config);
        };
        return this;
    }
    
    /// <summary>
    /// Finish configuring this entity and return to parent builder
    /// </summary>
    public FluentOverrideBuilder And()
    {
        _parent.AddEntry(_entry);
        return _parent;
    }
    
    /// <summary>
    /// Apply the configuration
    /// </summary>
    public void Apply()
    {
        _parent.AddEntry(_entry);
        _parent.Apply();
    }
}

/// <summary>
/// Options for in-memory repository configuration
/// </summary>
public class InMemoryOptions
{
    private readonly RepositoryConfiguration _config;
    
    internal InMemoryOptions(RepositoryConfiguration config)
    {
        _config = config;
    }
    
    public bool PersistAcrossSessions 
    { 
        get => _config.PersistAcrossSessions;
        set => _config.PersistAcrossSessions = value;
    }
}

/// <summary>
/// Options for database repository configuration
/// </summary>
public class DatabaseOptions
{
    private readonly RepositoryConfiguration _config;
    
    internal DatabaseOptions(RepositoryConfiguration config)
    {
        _config = config;
    }
    
    public string? ConnectionString 
    { 
        get => _config.ConnectionString;
        set => _config.ConnectionString = value;
    }
    
    public string? ProviderName 
    { 
        get => _config.ProviderName;
        set => _config.ProviderName = value;
    }
    
    public bool EnableWalMode 
    { 
        get => _config.EnableWalMode;
        set => _config.EnableWalMode = value;
    }
    
    public int BulkBatchSize 
    { 
        get => _config.BulkBatchSize;
        set => _config.BulkBatchSize = value;
    }
}

/// <summary>
/// Options for cached repository configuration
/// </summary>
public class CachedOptions
{
    private readonly RepositoryConfiguration _config;
    
    internal CachedOptions(RepositoryConfiguration config)
    {
        _config = config;
    }
    
    public int? CacheExpirationMinutes 
    { 
        get => _config.CacheExpirationMinutes;
        set => _config.CacheExpirationMinutes = value;
    }
    
    public bool PreloadCache 
    { 
        get => _config.PreloadCache;
        set => _config.PreloadCache = value;
    }
    
    public string? ConnectionString 
    { 
        get => _config.ConnectionString;
        set => _config.ConnectionString = value;
    }
}