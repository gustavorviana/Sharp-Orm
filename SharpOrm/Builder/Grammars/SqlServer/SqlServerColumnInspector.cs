using SharpOrm.SqlMethods;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// SQL Server implementation for inspecting column metadata.
    /// </summary>
    public class SqlServerColumnInspector : IColumnInspector
    {
        private readonly TableGrammar _grammar;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerColumnInspector"/> class.
        /// </summary>
        /// <param name="grammar">The table grammar instance.</param>
        public SqlServerColumnInspector(TableGrammar grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        /// <summary>
        /// Creates a SQL expression for retrieving column metadata from SQL Server.
        /// </summary>
        /// <returns>The SQL expression for retrieving column information.</returns>
        public SqlExpression GetColumnsQuery()
        {
            var tableName = _grammar.Name.Name;
            string[] parts = tableName.Contains(".") ? tableName.Split('.') : new[] { null, tableName };
            string schema = parts[0];
            string table = parts[1];

            // Check if it's a temporary table (starts with #)
            if (table.StartsWith("#"))
                return GetTempTableColumnsQuery(table);

            var query = @"
SELECT
    c.COLUMN_NAME as ColumnName,
    c.DATA_TYPE as DataType,
    c.CHARACTER_MAXIMUM_LENGTH as MaxLength,
    c.NUMERIC_PRECISION as [Precision],
    c.NUMERIC_SCALE as Scale,
    CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END as IsNullable,
    c.ORDINAL_POSITION as OrdinalPosition,
    c.COLUMN_DEFAULT as DefaultValue,
    c.COLLATION_NAME as Collation,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
    ISNULL(COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity'), 0) as IsIdentity,
    ISNULL(COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsComputed'), 0) as IsComputed,
    NULL as Comment
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
        AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
        AND tc.TABLE_NAME = ku.TABLE_NAME
) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
    AND c.TABLE_NAME = pk.TABLE_NAME
    AND c.COLUMN_NAME = pk.COLUMN_NAME
WHERE c.TABLE_NAME = ?";

            if (!string.IsNullOrEmpty(schema))
                query += " AND c.TABLE_SCHEMA = ?";

            query += " ORDER BY c.ORDINAL_POSITION";

            return string.IsNullOrEmpty(schema)
                ? new SqlExpression(query, table)
                : new SqlExpression(query, table, schema);
        }

        /// <summary>
        /// Creates a SQL expression for retrieving column metadata from temporary tables in SQL Server.
        /// </summary>
        /// <param name="tableName">The temporary table name (including # prefix).</param>
        /// <returns>The SQL expression for retrieving column information from tempdb.</returns>
        private static SqlExpression GetTempTableColumnsQuery(string tableName)
        {
            var query = @"
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS [Precision],
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.column_id AS OrdinalPosition,
    c.is_identity AS IsIdentity,
    c.is_computed AS IsComputed,
    dc.definition AS DefaultValue,
    c.collation_name AS Collation,
    CASE WHEN pk.index_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
    ep.value AS Comment
FROM tempdb.sys.columns AS c
INNER JOIN tempdb.sys.types AS t 
    ON c.user_type_id = t.user_type_id
INNER JOIN tempdb.sys.objects AS o 
    ON c.object_id = o.object_id
LEFT JOIN tempdb.sys.default_constraints AS dc 
    ON c.default_object_id = dc.object_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id, i.index_id
    FROM tempdb.sys.index_columns AS ic
    INNER JOIN tempdb.sys.indexes AS i 
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) AS pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
LEFT JOIN sys.extended_properties AS ep 
    ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
WHERE o.name LIKE ? + '%'
ORDER BY c.column_id;
";

            return new SqlExpression(query, tableName);
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
                var columnName = rows.GetString("ColumnName");
                var dataType = rows.GetString("DataType");
                var isNullable = rows.GetBoolean("IsNullable");
                var ordinalPosition = rows.GetInt32("OrdinalPosition");
                int? maxLength = rows.GetNullableInt32("MaxLength");
                int? precision = rows.GetNullableInt32("Precision");
                var scale = rows.GetNullableInt32("Scale");
                var isPrimaryKey = rows.GetBoolean("IsPrimaryKey");
                var isIdentity = rows.GetBoolean("IsIdentity");
                var isComputed = rows.GetBoolean("IsComputed");
                var defaultValue = rows.GetString("DefaultValue");
                var collation = rows.GetString("Collation");
                var comment = rows.GetString("Comment");

                if (dataType.EqualsIgnoreCase("int"))
                {
                    maxLength = null;
                    precision = null;
                }

                result.Add(new DbColumnInfo(
                    columnName: columnName,
                    dataType: dataType,
                    isNullable: isNullable,
                    ordinalPosition: ordinalPosition,
                    maxLength: maxLength,
                    precision: precision,
                    scale: scale,
                    isPrimaryKey: isPrimaryKey,
                    isIdentity: isIdentity,
                    isComputed: isComputed,
                    defaultValue: defaultValue,
                    collation: collation,
                    comment: comment
                ));
            }

            return result.ToArray();
        }
    }
}
