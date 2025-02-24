using System;
using System.Data;
using System.IO;
using System.Linq;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQL Server.
    /// </summary>
    public class SqlServerTableGrammar : TableGrammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public SqlServerTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        protected override DbName LoadName()
        {
            bool isTempName = Schema.Name.StartsWith("#");
            if (isTempName && !Schema.Temporary)
                throw new InvalidOperationException("The table name cannot start with '#'.");

            if (Schema.Name.EndsWith("_"))
                throw new NotSupportedException("The table name cannot end with '_'.");

            if (!Schema.Temporary)
                return new DbName(Schema.Name, string.Empty);

            if (Schema.Name.Contains("."))
                throw new NotSupportedException("A temporary table cannot contain '.' in its name.");

            if (Schema.Name.Length > 115)
                throw new InvalidOperationException("The table name must contain up to 115 characters.");

            if (isTempName) return new DbName(Schema.Name, string.Empty, false);

            return new DbName(string.Concat("#", Schema.Name), string.Empty);
        }

        public override SqlExpression Create()
        {
            if (Schema.BasedQuery != null)
                return CreateBased();

            var query = GetBuilder()
                .AddFormat("CREATE TABLE [{0}] (", Name.Name)
                .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));

            WriteUnique(query);
            WritePk(query);

            return query.Add(')').ToExpression();
        }

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException("The column name cannot contain \".\"");

            long seed = column.AutoIncrementSeed;
            if (seed <= 0)
                seed = 1;

            long step = column.AutoIncrementStep;
            if (step <= 0)
                step = 1;

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetSqlDataType(column);
            string identity = column.AutoIncrement ? string.Concat(" IDENTITY(", seed, ",", step, ")") : string.Empty;
            string nullable = column.AllowDBNull ? "NULL" : "NOT NULL";

            return string.Concat(columnName, " ", dataType, identity, " ", nullable);
        }

        private string GetSqlDataType(DataColumn column)
        {
            if (GetCustomColumnTypeMap(column) is ColumnTypeMap map)
                return map.GetTypeString(column);

            if (GetExpectedColumnType(column) is string typeColumn)
                return typeColumn;

            var dataType = column.DataType;
            if (dataType == typeof(int))
                return "INT";

            if (dataType == typeof(long))
                return "BIGINT";

            if (dataType == typeof(short))
                return "SMALLINT";

            if (dataType == typeof(byte))
                return "TINYINT";

            if (dataType == typeof(float))
                return "REAL";

            if (dataType == typeof(double))
                return "FLOAT";

            if (dataType == typeof(decimal))
                return "DECIMAL";

            if (dataType == typeof(bool))
                return "BIT";

            if (dataType == typeof(string))
                return string.Concat("VARCHAR(", column.MaxLength < 1 ? "MAX" : (object)column.MaxLength, ")");

            if (dataType == typeof(char))
                return "NCHAR(1)";

            if (dataType == typeof(DateTime))
                return "DATETIME";

            if (dataType == typeof(TimeSpan))
                return "TIME";

            if (dataType == typeof(byte[]) || dataType == typeof(MemoryStream))
                return "VARBINARY(MAX)";

            if (dataType == typeof(Guid))
                return "UNIQUEIDENTIFIER";

            throw new ArgumentException($"Unsupported data type: {dataType.Name}");
        }

        private SqlExpression CreateBased()
        {
            QueryBuilder query = GetBuilder();
            query.Add("SELECT ");

            if (Schema.BasedQuery.Limit is int limit && Schema.BasedQuery.Offset is null)
                query.Add($"TOP(").Add(limit).Add(") ");

            WriteColumns(query, BasedTable.Select);

            query.Add(" INTO [").Add(Name).Add("]");

            var qGrammar = new SqlServerSelectGrammar(Schema.BasedQuery);
            query.Add(qGrammar.GetSelectFrom());

            return query.ToExpression();
        }

        public override SqlExpression Exists()
        {
            if (Schema.Temporary)
                return new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE xtype = 'u' AND id = object_id('tempdb..' + ?)", Name.Name);

            var query = GetBuilder();
            query.Add("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE");

            var name = GetSplittedName();
            if (!string.IsNullOrEmpty(name[0]))
                query.Add(" TABLE_SCHEMA = ? AND", name[0]);

            query.Add(" TABLE_NAME = ?;", name[1]);

            return query.ToExpression();
        }

        private string[] GetSplittedName()
        {
            if (!Schema.Name.Contains("."))
                return new string[] { null, Schema.Name };

            return Schema.Name.Split('.');
        }
    }
}
