using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    /// <summary>
    /// SQLite implementation for inspecting column metadata.
    /// </summary>
    public class SqliteColumnInspector : IColumnInspector
    {
        private readonly TableGrammar _grammar;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteColumnInspector"/> class.
        /// </summary>
        /// <param name="grammar">The table grammar instance.</param>
        public SqliteColumnInspector(TableGrammar grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        /// <summary>
        /// Creates a SQL expression for retrieving column metadata from SQLite.
        /// </summary>
        /// <returns>The SQL expression for retrieving column information.</returns>
        public SqlExpression GetColumnsQuery()
        {
            var tableName = _grammar.Name.Name;

            // SQLite uses PRAGMA table_info to get column information
            var query = $"PRAGMA table_info({tableName})";

            return new SqlExpression(query);
        }

        /// <summary>
        /// Maps rows from the columns query result to an array of <see cref="DbColumnInfo"/> objects.
        /// </summary>
        /// <param name="rows">The row collection from the query result.</param>
        /// <returns>An array of <see cref="DbColumnInfo"/> instances with column metadata.</returns>
        public DbColumnInfo[] MapToColumnInfo(RowDataReader rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var result = new List<DbColumnInfo>();

            while (rows.Read())
            {
                // PRAGMA table_info returns:
                // cid, name, type, notnull, dflt_value, pk

                var dataType = rows.GetString("type");
                var columnName = rows.GetString("name");

                // Parse data type to extract size/precision info
                var typeInfo = ColumnTypeParser.Parse(dataType);

                result.Add(new DbColumnInfo(
                    columnName: columnName,
                    dataType: typeInfo.DataType ?? dataType,
                    isNullable: !rows.GetBoolean("notnull"),
                    ordinalPosition: rows.GetInt32("cid") + 1, // cid is 0-based, OrdinalPosition is 1-based
                    maxLength: typeInfo.MaxLength,
                    precision: typeInfo.Precision,
                    scale: typeInfo.Scale,
                    isPrimaryKey: rows.GetBoolean("pk"),
                    isIdentity: false, // SQLite doesn't have a direct way to detect this from PRAGMA table_info
                    isComputed: false, // SQLite doesn't expose this easily
                    defaultValue: rows.GetString("dflt_value"),
                    collation: null, // Not available in PRAGMA table_info
                    comment: null // Not available in PRAGMA table_info
                ));
            }

            return result.ToArray();
        }
    }
}
