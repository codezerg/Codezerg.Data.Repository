using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Data;

namespace Codezerg.Data.Repository.Migration
{
    /// <summary>
    /// Inspects database schema to query tables, columns, and their metadata.
    /// </summary>
    internal class SchemaInspector
    {
        private readonly DataConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaInspector"/> class.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        public SchemaInspector(DataConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Checks if a table exists in the database.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <returns>True if the table exists; otherwise, false.</returns>
        public bool TableExists(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

            try
            {
                // SQLite-specific query to check table existence
                var query = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                var result = _connection.Execute<int>(query);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all columns for a table from the database.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <returns>Collection of table columns.</returns>
        public IEnumerable<TableColumn> GetTableColumns(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

            try
            {
                // SQLite-specific: Use PRAGMA table_info to get column information
                var query = $"PRAGMA table_info({tableName})";
                var result = _connection.Query<SqliteColumnInfo>(query);

                return result.Select(row => new TableColumn(
                    columnName: row.name,
                    dataType: row.type,
                    isNullable: row.notnull == 0
                )).ToList();
            }
            catch
            {
                return Enumerable.Empty<TableColumn>();
            }
        }

        /// <summary>
        /// SQLite column info structure from PRAGMA table_info.
        /// </summary>
        private class SqliteColumnInfo
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }
    }
}
