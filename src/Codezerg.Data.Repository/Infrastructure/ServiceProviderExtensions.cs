using Microsoft.Extensions.DependencyInjection;
using System;
using Codezerg.Data.Repository.Core;

namespace Codezerg.Data.Repository.Infrastructure;

public static class ServiceProviderExtensions
{
    public static IRepository<T> GetRepository<T>(this IServiceProvider services) where T : class, new()
    {
        return services.GetRequiredService<IRepository<T>>();
    }
}
