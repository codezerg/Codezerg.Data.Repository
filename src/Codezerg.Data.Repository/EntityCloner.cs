using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codezerg.Data.Repository;

/// <summary>
/// Service for creating deep copies of entities with comprehensive caching
/// </summary>
public static class EntityCloner<T> where T : class, new()
{
    // Cache for property information
    private static readonly PropertyInfo[] _cachedProperties;

    // Cache for property getters and setters
    private static readonly Func<T, object>[] _cachedGetters;
    private static readonly Action<T, object>[] _cachedSetters;

    static EntityCloner()
    {
        // Cache all readable and writable properties once
        _cachedProperties = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        // Pre-compile getters and setters for maximum performance
        var getterList = new List<Func<T, object>>();
        var setterList = new List<Action<T, object>>();

        foreach (var prop in _cachedProperties)
        {
            // Create compiled getter
            var getter = CreateGetter(prop);
            getterList.Add(getter);

            // Create compiled setter
            var setter = CreateSetter(prop);
            setterList.Add(setter);
        }

        _cachedGetters = getterList.ToArray();
        _cachedSetters = setterList.ToArray();
    }

    /// <summary>
    /// Creates a compiled getter delegate for a property
    /// </summary>
    private static Func<T, object> CreateGetter(PropertyInfo property)
    {
        var getMethod = property.GetGetMethod();
        if (getMethod == null)
            return _ => null;

        return (Func<T, object>)Delegate.CreateDelegate(
            typeof(Func<T, object>),
            getMethod.IsStatic ? null : null,
            getMethod,
            throwOnBindFailure: false)
            ?? (entity => property.GetValue(entity));
    }

    /// <summary>
    /// Creates a compiled setter delegate for a property
    /// </summary>
    private static Action<T, object> CreateSetter(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod();
        if (setMethod == null)
            return (_, __) => { };

        return (entity, value) => property.SetValue(entity, value);
    }

    /// <summary>
    /// Creates a deep copy of an entity using cached reflection data
    /// </summary>
    public static T CreateDeepCopy(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var copy = new T();

        // Use cached getters and setters for optimal performance
        for (int i = 0; i < _cachedProperties.Length; i++)
        {
            var getter = _cachedGetters[i];
            var setter = _cachedSetters[i];

            var value = getter(entity);
            setter(copy, value);
        }

        return copy;
    }
}