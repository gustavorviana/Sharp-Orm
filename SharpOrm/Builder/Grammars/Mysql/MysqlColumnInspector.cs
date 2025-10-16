using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Mysql
{
    /// <summary>
    /// MySQL implementation for inspecting column metadata.
    /// </summary>
    public class MysqlColumnInspector : IColumnInspector
    {
        private readonly TableGrammar _grammar;

        /// <summary>
        /// Initializes a new instance of the <see cref="MysqlColumnInspector"/> class.
        /// </summary>
        /// <param name="grammar">The table grammar instance.</param>
        public MysqlColumnInspector(TableGrammar grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        /// <summary>
        /// Creates a SQL expression for retrieving column metadata from MySQL.
        /// </summary>
        /// <returns>The SQL expression for retrieving column information.</returns>
        public SqlExpression GetColumnsQuery()
        {
            var tableName = _grammar.Name.Name;

            // For temporary tables, use SHOW FULL COLUMNS as INFORMATION_SCHEMA doesn't work reliably
            if (_grammar.Schema.Temporary)
            {
                var quotedTableName = _grammar.Config.ApplyNomenclature(tableName);
                return new SqlExpression($"SHOW FULL COLUMNS FROM {quotedTableName}");
            }

            string[] parts = tableName.Contains(".") ? tableName.Split('.') : new[] { null, tableName };
            string schema = parts[0];
            string table = parts[1];

            var query = @"
SELECT
    c.COLUMN_NAME as ColumnName,
    c.DATA_TYPE as DataType,
    c.CHARACTER_MAXIMUM_LENGTH as MaxLength,
    c.NUMERIC_PRECISION as `Precision`,
    c.NUMERIC_SCALE as Scale,
    CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END as IsNullable,
    c.ORDINAL_POSITION as OrdinalPosition,
    c.COLUMN_DEFAULT as DefaultValue,
    c.COLLATION_NAME as Collation,
    CASE WHEN c.COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END as IsPrimaryKey,
    CASE WHEN c.EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END as IsIdentity,
    CASE WHEN c.GENERATION_EXPRESSION IS NOT NULL AND c.GENERATION_EXPRESSION != '' THEN 1 ELSE 0 END as IsComputed,
    c.COLUMN_COMMENT as Comment
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = ?";

            if (!string.IsNullOrEmpty(schema))
                query += " AND c.TABLE_SCHEMA = ?";
            else
                query += " AND c.TABLE_SCHEMA = DATABASE()";

            query += " ORDER BY c.ORDINAL_POSITION";

            return string.IsNullOrEmpty(schema)
                ? new SqlExpression(query, table)
                : new SqlExpression(query, table, schema);
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

            // Check if this is SHOW COLUMNS format (Field column exists) or INFORMATION_SCHEMA format (ColumnName exists)
            bool isShowColumnsFormat = rows.HasColumn("Field");

            var result = new List<DbColumnInfo>();
            int rowIndex = 0;

            while (rows.Read())
            {
                if (isShowColumnsFormat)
                {
                    result.Add(MapFromShowColumns(rows, rowIndex + 1));
                }
                else
                {
                    result.Add(new DbColumnInfo(
                        columnName: rows.GetString("ColumnName"),
                        dataType: rows.GetString("DataType"),
                        isNullable: rows.GetBoolean("IsNullable"),
                        ordinalPosition: rows.GetInt32("OrdinalPosition"),
                        maxLength: rows.GetNullableInt32("MaxLength"),
                        precision: rows.GetNullableInt32("Precision"),
                        scale: rows.GetNullableInt32("Scale"),
                        isPrimaryKey: rows.GetBoolean("IsPrimaryKey"),
                        isIdentity: rows.GetBoolean("IsIdentity"),
                        isComputed: rows.GetBoolean("IsComputed"),
                        defaultValue: rows.GetString("DefaultValue"),
                        collation: rows.GetString("Collation"),
                        comment: rows.GetString("Comment")
                    ));
                }
                rowIndex++;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Maps a row from SHOW FULL COLUMNS result to a <see cref="DbColumnInfo"/> object.
        /// </summary>
        private DbColumnInfo MapFromShowColumns(RowDataReader rows, int ordinalPosition)
        {
            var columnName = rows.GetString("Field");
            var typeString = rows.GetString("Type") ?? "";
            var nullString = rows.GetString("Null");
            var keyString = rows.GetString("Key");
            var defaultValue = rows.GetString("Default");
            var extraString = rows.GetString("Extra") ?? "";
            var collation = rows.GetString("Collation");
            var comment = rows.GetString("Comment");

            // Parse type string (e.g., "int(11)", "varchar(100)", "decimal(10,2)")
            var typeInfo = ColumnTypeParser.Parse(typeString);

            return new DbColumnInfo(
                columnName: columnName,
                dataType: typeInfo.DataType,
                isNullable: nullString?.Equals("YES", StringComparison.OrdinalIgnoreCase) ?? false,
                ordinalPosition: ordinalPosition,
                maxLength: typeInfo.MaxLength,
                precision: typeInfo.Precision,
                scale: typeInfo.Scale,
                isPrimaryKey: keyString?.Equals("PRI", StringComparison.OrdinalIgnoreCase) ?? false,
                isIdentity: extraString.IndexOf("auto_increment", StringComparison.OrdinalIgnoreCase) >= 0,
                isComputed: false, // SHOW FULL COLUMNS doesn't provide computed column info directly
                defaultValue: defaultValue,
                collation: collation,
                comment: comment
            );
        }
    }
}
