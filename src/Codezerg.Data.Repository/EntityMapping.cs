using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.Mapping;

namespace Codezerg.Data.Repository
{
    /// <summary>
    /// Provides mapping functionality for entities with automatic attribute mapping
    /// </summary>
    public static class EntityMapping<T> where T : class
    {
        private static MappingSchema _mappingSchema;
        private static string _tableName;
        private static string _database;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the table name for an entity type
        /// </summary>
        public static string GetTableName()
        {
            if (_tableName != null)
                return _tableName;

            var entityType = typeof(T);

            // Check for LinqToDB TableAttribute
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>(true);
            if (tableAttr?.Name != null)
            {
                _tableName = tableAttr.Name;
            }
            else
            {
                _tableName = entityType.Name;
            }

            return _tableName;
        }

        /// <summary>
        /// Gets the database name for an entity type
        /// </summary>
        public static string GetDatabaseName()
        {
            if (_database != null)
                return _database;

            var entityType = typeof(T);

            // Check for LinqToDB TableAttribute with Database property
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>(true);
            if (!string.IsNullOrEmpty(tableAttr?.Database))
            {
                _database = tableAttr.Database;
            }
            else
            {
                var assemblyName = entityType.Assembly.GetName().Name ?? "Default";
                _database = assemblyName;
            }

            return _database;
        }

        /// <summary>
        /// Gets the mapping schema for an entity type with automatic property mapping
        /// </summary>
        public static MappingSchema GetMappingSchema()
        {
            if (_mappingSchema != null)
                return _mappingSchema;

            lock (_lock)
            {
                if (_mappingSchema != null)
                    return _mappingSchema;

                // Create a new mapping schema and fluent builder
                _mappingSchema = new MappingSchema();
                var builder = new FluentMappingBuilder(_mappingSchema);
                var entityType = typeof(T);
                var entityBuilder = builder.Entity<T>();

                // Set table name
                var tableName = GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    entityBuilder.HasTableName(tableName);
                }

                // Process all public properties
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite);

                foreach (var property in properties)
                {
                    ProcessProperty(entityBuilder, property);
                }

                // Build the mappings
                builder.Build();
            }

            return _mappingSchema;
        }

        private static void ProcessProperty(EntityMappingBuilder<T> entityBuilder, PropertyInfo property)
        {
            // Check if property has a Column attribute specifically
            // We still need to process properties with PrimaryKey/Identity but no Column attribute
            var hasColumnAttribute = property.GetCustomAttribute<ColumnAttribute>() != null;
            var hasNotColumnAttribute = property.GetCustomAttribute<NotColumnAttribute>() != null;

            if (hasColumnAttribute || hasNotColumnAttribute)
            {
                // Column or NotColumn attributes are already there and will be used automatically
                return;
            }


            // Skip read-only properties (computed properties)
            if (!property.CanWrite)
            {
                entityBuilder.HasAttribute(property, new NotColumnAttribute());
                return;
            }

            // Skip navigation properties (collections and complex types that are not primitive)
            var propertyType = property.PropertyType;
            if (propertyType != typeof(string) &&
                (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                 propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                 propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                 propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                 propertyType.IsArray))
            {
                // Skip collection properties - they are navigation properties
                entityBuilder.HasAttribute(property, new NotColumnAttribute());
                return;
            }


            // Build column attribute with all settings
            var columnAttribute = new ColumnAttribute();
            bool hasColumnSettings = false;

            // Use property name as column name by default
            columnAttribute.Name = property.Name;
            hasColumnSettings = true;

            // Check nullability based on type
            if (IsNullableType(property.PropertyType))
            {
                columnAttribute.CanBeNull = true;
                hasColumnSettings = true;
            }
            else
            {
                // Value types are not nullable by default
                columnAttribute.CanBeNull = false;
                hasColumnSettings = true;
            }


            // Handle enum types - they need explicit DataType
            if (property.PropertyType.IsEnum)
            {
                columnAttribute.DataType = LinqToDB.DataType.Int32;
                hasColumnSettings = true;
            }
            else if (Nullable.GetUnderlyingType(property.PropertyType)?.IsEnum == true)
            {
                columnAttribute.DataType = LinqToDB.DataType.Int32;
                hasColumnSettings = true;
            }

            // Apply the column attribute - we always need it since we've already filtered out
            // properties that have explicit Column or NotColumn attributes
            entityBuilder.HasAttribute(property, columnAttribute);
        }

        private static bool IsNullableType(Type type)
        {
            if (!type.IsValueType) return true; // Reference types are nullable
            return Nullable.GetUnderlyingType(type) != null; // Check for Nullable<T>
        }
    }
}