using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
                // Check for System.ComponentModel.DataAnnotations.Schema.TableAttribute
                var dataAnnotationsTableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>(true);
                _tableName = dataAnnotationsTableAttr?.Name ?? entityType.Name;
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

            // Check for NotMapped attribute first
            var notMappedAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>();
            if (notMappedAttr != null)
            {
                // Add NotColumn attribute to exclude from mapping
                entityBuilder.HasAttribute(property, new NotColumnAttribute());
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

            // Process DataAnnotations and translate them to linq2db attributes

            // Check if property already has PrimaryKey or Identity attributes from linq2db
            var hasPrimaryKey = property.GetCustomAttribute<PrimaryKeyAttribute>() != null;
            var hasIdentity = property.GetCustomAttribute<IdentityAttribute>() != null;

            // Key attribute -> PrimaryKey (only if not already present)
            var keyAttr = property.GetCustomAttribute<KeyAttribute>();
            if (keyAttr != null && !hasPrimaryKey)
            {
                entityBuilder.HasAttribute(property, new PrimaryKeyAttribute());
            }

            // DatabaseGenerated(Identity) -> Identity (only if not already present)
            var dbGeneratedAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute>();
            if (dbGeneratedAttr != null && dbGeneratedAttr.DatabaseGeneratedOption == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity && !hasIdentity)
            {
                entityBuilder.HasAttribute(property, new IdentityAttribute());
            }

            // Build column attribute with all settings
            var columnAttribute = new ColumnAttribute();
            bool hasColumnSettings = false;

            // Column name from DataAnnotations Column attribute
            var dataAnnotationsColumnAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
            if (dataAnnotationsColumnAttr != null)
            {
                if (!string.IsNullOrEmpty(dataAnnotationsColumnAttr.Name))
                {
                    columnAttribute.Name = dataAnnotationsColumnAttr.Name;
                    hasColumnSettings = true;
                }

                if (!string.IsNullOrEmpty(dataAnnotationsColumnAttr.TypeName))
                {
                    columnAttribute.DataType = LinqToDB.DataType.Undefined; // Will use database-specific type name
                    columnAttribute.DbType = dataAnnotationsColumnAttr.TypeName;
                    hasColumnSettings = true;
                }

                if (dataAnnotationsColumnAttr.Order >= 0)
                {
                    columnAttribute.Order = dataAnnotationsColumnAttr.Order;
                    hasColumnSettings = true;
                }
            }
            else
            {
                // Use property name as column name by default
                columnAttribute.Name = property.Name;
                hasColumnSettings = true;
            }

            // Required attribute -> NOT NULL
            var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttr != null)
            {
                columnAttribute.CanBeNull = false;
                hasColumnSettings = true;
            }
            else if (IsNullableType(property.PropertyType))
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

            // MaxLength/StringLength attributes -> Length
            var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
            var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();

            if (maxLengthAttr != null && maxLengthAttr.Length > 0)
            {
                columnAttribute.Length = maxLengthAttr.Length;
                hasColumnSettings = true;
            }
            else if (stringLengthAttr != null && stringLengthAttr.MaximumLength > 0)
            {
                columnAttribute.Length = stringLengthAttr.MaximumLength;
                hasColumnSettings = true;
            }

            // DataType attribute for special types
            var dataTypeAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.DataTypeAttribute>();
            if (dataTypeAttr != null && string.IsNullOrEmpty(columnAttribute.DbType))
            {
                switch (dataTypeAttr.DataType)
                {
                    case DataType.Date:
                        columnAttribute.DataType = LinqToDB.DataType.Date;
                        hasColumnSettings = true;
                        break;
                    case DataType.Time:
                        columnAttribute.DataType = LinqToDB.DataType.Time;
                        hasColumnSettings = true;
                        break;
                    case DataType.DateTime:
                        columnAttribute.DataType = LinqToDB.DataType.DateTime;
                        hasColumnSettings = true;
                        break;
                    case DataType.Currency:
                        columnAttribute.DataType = LinqToDB.DataType.Decimal;
                        columnAttribute.Precision = 19;
                        columnAttribute.Scale = 4;
                        hasColumnSettings = true;
                        break;
                    case DataType.Text:
                    case DataType.MultilineText:
                    case DataType.Html:
                        columnAttribute.DataType = LinqToDB.DataType.Text;
                        hasColumnSettings = true;
                        break;
                }
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