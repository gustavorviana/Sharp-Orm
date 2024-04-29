﻿using System;
using System.Data;
using System.IO;
using System.Linq;

namespace SharpOrm.Builder
{
    internal class SqlServerTableGrammar : TableGrammar
    {
        public override DbName Name
        {
            get
            {
                if (this.Schema.Temporary)
                    return new DbName($"#{this.Schema.Name}", "");

                return new DbName(this.Schema.Name, "");
            }
        }

        public SqlServerTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        public override SqlExpression Create()
        {
            if (this.Schema.Based != null)
                return this.CreateBased();

            return new SqlExpression($"CREATE TABLE {this.Name} ({string.Join(",", this.Schema.Columns.Select(GetColumnDefinition))})");
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
            string identity = column.Unique ? $"IDENTITY({seed},{step})" : "";
            string nullable = column.AllowDBNull ? "NULL" : "NOT NULL";

            return $"{columnName} {dataType} {identity} {nullable}";
        }

        private string GetSqlDataType(DataColumn column)
        {
            if (this.GetCustomColumnTypeMap(column) is ColumnTypeMap map)
                return map.GetTypeString(column);

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
                return string.Format("VARCHAR({0})", column.MaxLength < 1 ? (object)"MAX" : (object)column.MaxLength);

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
            QueryConstructor query = this.GetConstructor();
            query.Add("SELECT ");

            this.WriteColumns(query, this.Schema.Based.Columns);

            query.AddFormat(" INTO [{0}] FROM [{1}]", this.Name, this.Schema.Based.Name);

            if (!this.Schema.Based.CopyData)
                query.Add(" WHERE 1=2;");

            return query.ToExpression();
        }

        private void WriteColumns(QueryConstructor query, Column[] columns)
        {
            if (columns.Length == 0)
            {
                query.Add("*");
                return;
            }

            query.AddExpression(columns[0]);

            for (int i = 0; i < columns.Length; i++)
                query.Add(",").AddExpression(columns[i]);
        }

        public override SqlExpression Drop()
        {
            return new SqlExpression("DROP TABLE " + this.Name);
        }

        public override SqlExpression Count()
        {
            if (this.Schema.Temporary)
                return new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE charindex('_', name) > 0 AND left(name, charindex('_', name) -1) = ? AND xtype = 'u' AND object_id('tempdb..' + name) IS NOT NULL", this.Name.Name);

            var query = this.GetConstructor();
            query.Add("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE");

            if (!string.IsNullOrEmpty(this.Schema.SchemaName))
                query.Add(" TABLE_SCHEMA = ? AND", this.Schema.SchemaName);

            query.Add(" TABLE_NAME = ?;", this.Schema.Name);

            return query.ToExpression();
        }
    }
}