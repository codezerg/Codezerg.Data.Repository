using LinqToDB.Mapping;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codezerg.Data.Repository;

/// <summary>
/// Service for managing entity primary keys
/// </summary>
public class PrimaryKeyHelper<T> where T : class
{
    private readonly List<PropertyInfo> _primaryKeyProperties;

    public PrimaryKeyHelper()
    {
        _primaryKeyProperties = GetPrimaryKeyProperties().ToList();
    }

    /// <summary>
    /// Gets the primary key properties for the entity
    /// </summary>
    public IReadOnlyList<PropertyInfo> PrimaryKeyProperties => _primaryKeyProperties;

    /// <summary>
    /// Gets all primary key properties for the entity type
    /// </summary>
    private IReadOnlyList<PropertyInfo> GetPrimaryKeyProperties()
    {
        return typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes<PrimaryKeyAttribute>(true).Any())
            .ToList();
    }

    /// <summary>
    /// Creates a dictionary of primary key names and values for an entity
    /// </summary>
    public Dictionary<string, object> GetPrimaryKeyValues(T entity)
    {
        var result = new Dictionary<string, object>();

        foreach (var prop in _primaryKeyProperties)
        {
            result[prop.Name] = prop.GetValue(entity);
        }

        return result;
    }

    /// <summary>
    /// Determines if two entities have the same primary key values
    /// </summary>
    public bool HaveSamePrimaryKeys(T entity1, T entity2)
    {
        return _primaryKeyProperties.All(prop =>
        {
            var value1 = prop.GetValue(entity1);
            var value2 = prop.GetValue(entity2);

            return value1 != null &&
                   value2 != null &&
                   value1.Equals(value2);
        });
    }

    /// <summary>
    /// Finds an entity in a collection by its primary key values
    /// </summary>
    public T FindEntityByPrimaryKeys(IEnumerable<T> entities, T entity)
    {
        if (_primaryKeyProperties.Count == 0)
            return null;

        return entities.FirstOrDefault(e => HaveSamePrimaryKeys(e, entity));
    }
}