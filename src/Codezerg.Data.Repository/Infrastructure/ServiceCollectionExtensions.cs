using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Codezerg.Data.Repository.Core;
using Codezerg.Data.Repository.Core.Overrides;
using System;

namespace Codezerg.Data.Repository.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add repository services with default configuration
    /// </summary>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        return AddRepositoryServices(services, new RepositoryOptions());
    }
    
    /// <summary>
    /// Add repository services with custom options
    /// </summary>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services, RepositoryOptions options)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        services.AddSingleton(options);
        services.AddSingleton<IRepositoryOverrideManager, RepositoryOverrideManager>();
        
        // Register the attribute-aware factory if attribute configuration is enabled
        if (options.UseAttributeConfiguration)
        {
            services.AddSingleton<IRepositoryFactory, AttributeAwareRepositoryFactory>();
        }
        else
        {
            services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
        }
        
        services.AddSingleton(typeof(IRepository<>), typeof(RepositoryProxy<>));
        
        return services;
    }
    
    /// <summary>
    /// Add repository services with configuration action
    /// </summary>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services, Action<RepositoryOptions> configureOptions)
    {
        if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));
        
        var options = new RepositoryOptions();
        configureOptions(options);
        
        return AddRepositoryServices(services, options);
    }
    
    /// <summary>
    /// Add repository services with attribute configuration and overrides
    /// </summary>
    public static IServiceCollection AddRepositoryServicesWithAttributes(
        this IServiceCollection services,
        Action<RepositoryOptions>? configureOptions = null,
        Action<IRepositoryOverrideManager>? configureOverrides = null)
    {
        var options = new RepositoryOptions
        {
            UseAttributeConfiguration = true
        };
        
        configureOptions?.Invoke(options);
        
        services.AddSingleton(options);
        
        // Create and configure override manager
        var overrideManager = new RepositoryOverrideManager();
        configureOverrides?.Invoke(overrideManager);
        services.AddSingleton<IRepositoryOverrideManager>(overrideManager);
        
        // Register attribute-aware factory with override support
        services.AddSingleton<IRepositoryFactory>(provider =>
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            return new AttributeAwareRepositoryFactory(options, overrideManager, loggerFactory);
        });
        
        services.AddSingleton(typeof(IRepository<>), typeof(RepositoryProxy<>));
        
        return services;
    }
    
    /// <summary>
    /// Configure repository overrides using fluent builder
    /// </summary>
    public static IServiceCollection ConfigureRepositoryOverrides(
        this IServiceCollection services,
        Action<FluentOverrideBuilder> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        
        // Ensure override manager is registered
        services.AddSingleton<IRepositoryOverrideManager>(provider =>
        {
            var overrideManager = new RepositoryOverrideManager();
            var builder = new FluentOverrideBuilder(overrideManager);
            configure(builder);
            builder.Apply();
            return overrideManager;
        });
        
        return services;
    }
    
    /// <summary>
    /// Create a scoped repository context for temporary overrides
    /// </summary>
    public static ScopedRepositoryContext CreateScopedRepositoryContext(this IServiceProvider services)
    {
        var overrideManager = services.GetService<IRepositoryOverrideManager>();
        if (overrideManager == null)
        {
            throw new InvalidOperationException(
                "IRepositoryOverrideManager is not registered. " +
                "Call AddRepositoryServices or AddRepositoryServicesWithAttributes first.");
        }
        
        return new ScopedRepositoryContext(overrideManager);
    }
}