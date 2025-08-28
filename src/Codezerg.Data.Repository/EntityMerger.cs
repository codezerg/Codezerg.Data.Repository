using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codezerg.Data.Repository
{
    /// <summary>
    /// Service for updating entity properties
    /// </summary>
    public static class EntityMerger<T> where T : class
    {
        /// <summary>
        /// Updates properties of a target entity with values from a source entity
        /// </summary>
        public static void UpdateEntityProperties(T target, T source, IReadOnlyList<PropertyInfo> excludeProperties)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in properties)
            {
                // Skip excluded properties (like primary keys)
                if (excludeProperties.Contains(prop))
                    continue;

                var value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }
    }
}