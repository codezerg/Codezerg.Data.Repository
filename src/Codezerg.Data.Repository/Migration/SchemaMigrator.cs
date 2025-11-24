using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;

namespace Codezerg.Data.Repository.Migration
{
    /// <summary>
    /// Applies schema changes to the database (CREATE TABLE, ADD COLUMN, ALTER COLUMN).
    /// </summary>
    internal class SchemaMigrator
    {
        private readonly DataConnection _connection;
        private readonly SchemaInspector _inspector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaMigrator"/> class.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="inspector">The schema inspector.</param>
        public SchemaMigrator(DataConnection connection, SchemaInspector inspector)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        }

        /// <summary>
        /// Creates a table for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        public void CreateTable<T>() where T : class
        {
            _connection.CreateTable<T>(tableOptions: TableOptions.CreateIfNotExists);
        }

        /// <summary>
        /// Adds a new column to an existing table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="column">The column to add.</param>
        public void AddColumn(string tableName, TableColumn column)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

            if (column == null)
                throw new ArgumentNullException(nameof(column));

            var nullable = column.IsNullable ? "NULL" : "NOT NULL";
            var sql = $"ALTER TABLE [{tableName}] ADD COLUMN [{column.ColumnName}] {column.DataType} {nullable}";

            _connection.Execute(sql);
        }

        /// <summary>
        /// Alters a column definition (type or nullability).
        /// For SQLite, this uses the table recreation pattern since SQLite has limited ALTER COLUMN support.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnName">The column name to alter.</param>
        /// <param name="newColumn">The new column definition.</param>
        public void AlterColumn<T>(string tableName, string columnName, TableColumn newColumn) where T : class
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));

            if (newColumn == null)
                throw new ArgumentNullException(nameof(newColumn));

            // SQLite requires table recreation for ALTER COLUMN
            AlterColumnForSQLite<T>(tableName, columnName, newColumn);
        }

        /// <summary>
        /// Handles column alteration for SQLite by recreating the table.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnName">The column name to alter.</param>
        /// <param name="newColumn">The new column definition.</param>
        private void AlterColumnForSQLite<T>(string tableName, string columnName, TableColumn newColumn) where T : class
        {
            var tempTableName = $"{tableName}_temp_{Guid.NewGuid():N}";

            // Get current columns and replace the one being altered
            var columns = _inspector.GetTableColumns(tableName).ToList();
            var columnIndex = columns.FindIndex(c =>
                string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

            if (columnIndex < 0)
                throw new InvalidOperationException($"Column {columnName} not found in table {tableName}");

            // Replace with the new column definition, preserving the original column name
            var updatedColumn = new TableColumn(
                columnName: columns[columnIndex].ColumnName,
                dataType: newColumn.DataType,
                isNullable: newColumn.IsNullable
            );
            columns[columnIndex] = updatedColumn;

            // Build CREATE TABLE statement for temp table
            var columnDefinitions = columns.Select(c =>
                $"[{c.ColumnName}] {c.DataType} {(c.IsNullable ? "NULL" : "NOT NULL")}");

            var createTableSql = $"CREATE TABLE [{tempTableName}] ({string.Join(", ", columnDefinitions)})";

            // Execute table recreation pattern
            try
            {
                // 1. Create temporary table with new schema
                _connection.Execute(createTableSql);

                // 2. Copy data from original table
                var columnNames = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));
                var copySql = $"INSERT INTO [{tempTableName}] ({columnNames}) SELECT {columnNames} FROM [{tableName}]";
                _connection.Execute(copySql);

                // 3. Drop original table
                _connection.Execute($"DROP TABLE [{tableName}]");

                // 4. Rename temporary table to original name
                _connection.Execute($"ALTER TABLE [{tempTableName}] RENAME TO [{tableName}]");
            }
            catch (Exception)
            {
                // Try to clean up temp table if it exists
                try
                {
                    _connection.Execute($"DROP TABLE IF EXISTS [{tempTableName}]");
                }
                catch
                {
                    // Ignore cleanup errors
                }

                throw;
            }
        }
    }
}
