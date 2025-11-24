using System;

namespace Codezerg.Data.Repository.Migration
{
    /// <summary>
    /// Represents a database table column with its metadata.
    /// </summary>
    public class TableColumn
    {
        /// <summary>
        /// Gets the column name.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the data type of the column (e.g., "INTEGER", "TEXT", "REAL").
        /// </summary>
        public string DataType { get; }

        /// <summary>
        /// Gets a value indicating whether the column allows null values.
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumn"/> class.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="isNullable">Whether the column is nullable.</param>
        public TableColumn(string columnName, string dataType, bool isNullable)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(columnName));

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("Data type cannot be null or whitespace.", nameof(dataType));

            ColumnName = columnName;
            DataType = dataType;
            IsNullable = isNullable;
        }

        /// <summary>
        /// Returns a string representation of the column.
        /// </summary>
        public override string ToString()
        {
            return $"{ColumnName} {DataType} {(IsNullable ? "NULL" : "NOT NULL")}";
        }
    }
}
