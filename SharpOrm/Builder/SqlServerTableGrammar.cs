﻿using System;
using System.Data;
using System.IO;
using System.Linq;

namespace SharpOrm.Builder
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
            bool isTempName = this.Schema.Name.StartsWith("#");
            if (isTempName && !this.Schema.Temporary)
                throw new InvalidOperationException("The table name cannot start with '#'.");

            if (this.Schema.Name.EndsWith("_"))
                throw new NotSupportedException("The table name cannot end with '_'.");

            if (!this.Schema.Temporary)
                return new DbName(this.Schema.Name, "");

            if (Schema.Name.Contains("."))
                throw new NotSupportedException("A temporary table cannot contain '.' in its name.");

            if (Schema.Name.Length > 115)
                throw new InvalidOperationException("The table name must contain up to 115 characters.");

            if (isTempName) return new DbName(this.Schema.Name, "", false);

            return new DbName(string.Concat("#", this.Schema.Name), "");
        }

        public override SqlExpression Create()
        {
            if (this.Schema.BasedQuery != null)
                return this.CreateBased();

            var query = this.GetBuilder()
                .AddFormat("CREATE TABLE [{0}] (", this.Name.Name)
                .AddJoin(",", this.Schema.Columns.Select(GetColumnDefinition));

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
            string identity = column.AutoIncrement ? string.Concat(" IDENTITY(", seed, ",", step, ")") : "";
            string nullable = column.AllowDBNull ? "NULL" : "NOT NULL";

            return string.Concat(columnName, " ", dataType, identity, " ", nullable);
        }

        private string GetSqlDataType(DataColumn column)
        {
            if (this.GetCustomColumnTypeMap(column) is ColumnTypeMap map)
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
                return string.Concat("VARCHAR(", column.MaxLength < 1 ? (object)"MAX" : (object)column.MaxLength, ")");

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
            QueryBuilder query = this.GetBuilder();
            query.Add("SELECT ");

            if (this.Schema.BasedQuery.Limit is int limit && this.Schema.BasedQuery.Offset is null)
                query.Add($"TOP(").Add(limit).Add(") ");

            this.WriteColumns(query, this.BasedTable.Select);

            query.Add(" INTO [").Add(this.Name).Add("]");

            var qGrammar = new SqlServerGrammar(this.Schema.BasedQuery);
            query.Add(qGrammar.GetSelectFrom());

            return query.ToExpression();
        }

        public override SqlExpression Exists()
        {
            if (this.Schema.Temporary)
                return new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE xtype = 'u' AND object_id('tempdb..' + name) IS NOT NULL AND LEFT(name,LEN(name) - PATINDEX('%[^_]%', REVERSE(LEFT(name, LEN(name) - 12))) - 11) = ?", this.Name.Name);

            var query = this.GetBuilder();
            query.Add("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE");

            var name = GetSplittedName();
            if (!string.IsNullOrEmpty(name[0]))
                query.Add(" TABLE_SCHEMA = ? AND", name[0]);

            query.Add(" TABLE_NAME = ?;", name[1]);

            return query.ToExpression();
        }

        private string[] GetSplittedName()
        {
            if (!this.Schema.Name.Contains("."))
                return new string[] { null, this.Schema.Name };

            return this.Schema.Name.Split('.');
        }
    }
}
