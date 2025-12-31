using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace Codezerg.Data.Repository.Migration
{
    /// <summary>
    /// Manages automatic schema migrations for repository entities.
    /// Detects schema changes and applies them transparently.
    /// </summary>
    internal static class SchemaManager<T> where T : class
    {
        private static readonly object _lock = new object();
        private static readonly HashSet<string> _ensuredConnections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ensures the database schema matches the entity definition.
        /// Creates tables if missing, adds columns if needed, and alters columns if types/nullability changed.
        /// This method is thread-safe and only executes once per entity type.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="mappingSchema">The mapping schema for the entity.</param>
        public static void EnsureSchema(DataConnection connection, MappingSchema mappingSchema)
        {
            var connectionKey = connection.ConnectionString ?? string.Empty;

            if (_ensuredConnections.Contains(connectionKey))
                return;

            lock (_lock)
            {
                if (_ensuredConnections.Contains(connectionKey))
                    return;

                try
                {
                    var inspector = new SchemaInspector(connection);
                    var migrator = new SchemaMigrator(connection, inspector);

                    var tableName = EntityMapping<T>.GetTableName();
                    var tableExists = inspector.TableExists(tableName);

                    if (!tableExists)
                    {
                        // Table doesn't exist - create it
                        migrator.CreateTable<T>();
                        _ensuredConnections.Add(connectionKey);
                        return;
                    }

                    // Table exists - check for schema differences
                    var entityColumns = GetEntityColumns(connection, mappingSchema);
                    var tableColumns = inspector.GetTableColumns(tableName).ToList();

                    foreach (var entityColumn in entityColumns)
                    {
                        var tableColumn = tableColumns.FirstOrDefault(c =>
                            string.Equals(c.ColumnName, entityColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

                        if (tableColumn == null)
                        {
                            // Column missing - add it
                            migrator.AddColumn(tableName, entityColumn);
                        }
                        else if (!AreColumnsEqual(tableColumn, entityColumn))
                        {
                            // Column exists but definition differs - alter it
                            migrator.AlterColumn<T>(tableName, entityColumn.ColumnName, entityColumn);
                        }
                    }

                    _ensuredConnections.Add(connectionKey);
                }
                catch (Exception)
                {
                    // If schema migration fails, don't mark as ensured so it can be retried
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the columns for the entity type from the mapping schema.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="mappingSchema">The mapping schema.</param>
        /// <returns>Collection of table columns.</returns>
        private static IEnumerable<TableColumn> GetEntityColumns(DataConnection connection, MappingSchema mappingSchema)
        {
            var descriptor = mappingSchema.GetEntityDescriptor(typeof(T));

            return descriptor.Columns
                .Select(c => new TableColumn(
                    columnName: c.ColumnName,
                    dataType: GetColumnDbType(connection, mappingSchema, c),
                    isNullable: c.CanBeNull
                ))
                .ToList();
        }

        /// <summary>
        /// Gets the database-specific type string for a column.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="mappingSchema">The mapping schema.</param>
        /// <param name="column">The column descriptor.</param>
        /// <returns>The database type string (e.g., "INTEGER", "TEXT", "REAL").</returns>
        private static string GetColumnDbType(DataConnection connection, MappingSchema mappingSchema, ColumnDescriptor column)
        {
            try
            {
                var dataProvider = connection.DataProvider;
                var sqlBuilder = dataProvider.CreateSqlBuilder(mappingSchema, connection.Options);

                if (sqlBuilder != null)
                {
                    var dbDataType = column.GetDbDataType(true);
                    var sqlDataType = new SqlDataType(dbDataType);
                    var result = sqlBuilder.BuildDataType(new StringBuilder(), dbDataType).ToString();
                    return result;
                }
            }
            catch
            {
                // Fall through to default behavior
            }

            // Fallback to basic type mapping
            return column.DataType.ToString();
        }

        /// <summary>
        /// Compares two columns to determine if they are equal.
        /// Considers data type and nullability.
        /// </summary>
        /// <param name="tableColumn">The column from the database.</param>
        /// <param name="entityColumn">The column from the entity definition.</param>
        /// <returns>True if columns are equal; otherwise, false.</returns>
        private static bool AreColumnsEqual(TableColumn tableColumn, TableColumn entityColumn)
        {
            // Normalize data types for comparison (case-insensitive, trim whitespace)
            var tableType = NormalizeDataType(tableColumn.DataType);
            var entityType = NormalizeDataType(entityColumn.DataType);

            return string.Equals(tableType, entityType, StringComparison.OrdinalIgnoreCase) &&
                   tableColumn.IsNullable == entityColumn.IsNullable;
        }

        /// <summary>
        /// Normalizes a data type string for comparison.
        /// </summary>
        /// <param name="dataType">The data type string.</param>
        /// <returns>The normalized data type.</returns>
        private static string NormalizeDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                return string.Empty;

            // Remove extra whitespace and convert to uppercase for comparison
            return dataType.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Resets the schema ensured state for testing purposes.
        /// </summary>
        internal static void ResetForTesting()
        {
            lock (_lock)
            {
                _ensuredConnections.Clear();
            }
        }
    }
}
