using System;
using System.Data;
using System.IO;
using System.Linq;

namespace SharpOrm.Builder
{
    internal class SqlServerTableGrammar : TableGrammar
    {
        private SqlServerQueryConfig Config => this.queryInfo.Config as SqlServerQueryConfig;

        public SqlServerTableGrammar(IReadonlyQueryInfo queryInfo) : base(queryInfo)
        {
        }

        public override SqlExpression Create(TableSchema table)
        {
            if (table.BasedTable != null)
                return this.CreateBased(table);

            return new SqlExpression($"CREATE TABLE {GetName(table)} ({string.Join(",", table.Columns.Select(GetColumnDefinition))})");
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

        private SqlExpression CreateBased(TableSchema table)
        {
            QueryConstructor query = this.GetConstructor();
            query.Add("SELECT ");

            this.WriteColumns(query, table.BasedTable.Columns);

            query.AddFormat(" INTO [{0}] FROM [{1}]", GetName(table), table.BasedTable.Name);

            if (!table.BasedTable.CopyData)
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

        public override SqlExpression Drop(TableSchema table)
        {
            return new SqlExpression("DROP TABLE " + GetName(table));
        }

        public override SqlExpression Count(TableSchema table)
        {
            if (table.Temporary)
                return new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE charindex('_', name) > 0 AND left(name, charindex('_', name) -1) = ? AND xtype = 'u' AND object_id('tempdb..' + name) IS NOT NULL", GetName(table).Name);

            var query = this.GetConstructor();
            query.Add("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE");

            if (!string.IsNullOrEmpty(table.SchemaName))
                query.Add(" TABLE_SCHEMA = ? AND", table.SchemaName);

            query.Add(" TABLE_NAME = ?;", table.Name);

            return query.ToExpression();
        }


        public override DbName GetName(TableSchema table)
        {
            if (table.Temporary)
                return new DbName($"#{table.Name}", "");

            return new DbName(table.Name, "");
        }
    }
}
