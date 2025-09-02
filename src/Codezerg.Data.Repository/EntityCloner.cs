using System;
using System.Linq;

namespace Codezerg.Data.Repository;

/// <summary>
/// Service for creating deep copies of entities
/// </summary>
public static class EntityCloner<T> where T : class, new()
{
    /// <summary>
    /// Creates a deep copy of an entity
    /// </summary>
    public static T CreateDeepCopy(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var copy = new T();
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(entity);
            prop.SetValue(copy, value);
        }

        return copy;
    }
}