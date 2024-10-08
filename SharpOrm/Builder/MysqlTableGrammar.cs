﻿using System;
using System.Data;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to MySQL.
    /// </summary>
    public class MysqlTableGrammar : TableGrammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MysqlTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public MysqlTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        public override SqlExpression Exists()
        {
            return new SqlExpression(new StringBuilder()
                .Append("CALL sys.table_exists(DATABASE(),")
                .Append('"')
                .Append(this.Name.Name)
                .Append('"')
                .Append(",@`table_type`);SELECT @`table_type`=")
                .Append('"')
                .Append(this.Schema.Temporary ? "TEMPORARY" : "BASE TABLE")
                .Append('"')
                .Append(';')
            );
        }

        public override SqlExpression Create()
        {
            if (this.Schema.BasedQuery != null)
                return this.CreateBased();

            var query = this.GetCreateTableQuery()
                .Add('(')
                .AddJoin(",", this.Schema.Columns.Select(GetColumnDefinition));

            WriteUnique(query);
            WritePk(query);

            return query.Add(')').ToExpression();
        }

        private SqlExpression CreateBased()
        {
            return this.GetCreateTableQuery()
                .Add(new MysqlGrammar(this.Schema.BasedQuery).Select())
                .ToExpression();
        }

        private QueryBuilder GetCreateTableQuery()
        {
            QueryBuilder query = this.GetBuilder();
            query.Add("CREATE ");

            if (this.Schema.Temporary)
                query.Add("TEMPORARY ");

            query.AddFormat("TABLE {0} ", this.ApplyNomenclature(this.Name.Name));

            return query;
        }

        public override SqlExpression Drop()
        {
            var query = this.GetBuilder();
            query.Add("DROP ");

            if (this.Schema.Temporary)
                query.Add("TEMPORARY ");

            query.Add("TABLE ").Add(this.ApplyNomenclature(this.Name.Name));

            return query.ToExpression();
        }

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException("The column name cannot contain \".\"");

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetMySqlDataType(column);
            string autoIncrement = column.AutoIncrement ? " AUTO_INCREMENT" : "";
            string nullable = column.AllowDBNull ? "DEFAULT NULL" : "NOT NULL";

            return string.Concat(columnName, " ", dataType, " ", nullable, autoIncrement);
        }

        //Ref: https://medium.com/dbconvert/mysql-and-sql-servers-data-types-mapping-4cedc95de638
        private string GetMySqlDataType(DataColumn column)
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
                return "FLOAT";

            if (dataType == typeof(double))
                return "DOUBLE";

            if (dataType == typeof(decimal))
                return "DECIMAL";

            if (dataType == typeof(bool))
                return "BIT";

            if (dataType == typeof(string))
                return GetStringType(column.MaxLength);

            if (dataType == typeof(char))
                return "CHAR(1)";

            if (dataType == typeof(DateTime))
                return "DATETIME";

            if (dataType == typeof(TimeSpan))
                return "TIME";

            if (dataType == typeof(byte[]))
                return "BLOB";

            if (dataType == typeof(Guid))
                return string.Concat("CHAR(", this.GetGuidSize(), ")");

            throw new ArgumentException($"Unsupported data type: {dataType.Name}");
        }

        private string GetStringType(int maxSize)
        {
            if (maxSize <= 0)
                maxSize = 65535;

            if (maxSize == 255)
                return "TINYTEXT";

            if (maxSize <= 16383)
                return string.Concat("VARCHAR(", maxSize, ")");

            if (maxSize <= 65635)
                return "TEXT";

            if (maxSize <= 16777215)
                return "MEDIUMTEXT";

            return "LONGTEXT";
        }
    }
}
