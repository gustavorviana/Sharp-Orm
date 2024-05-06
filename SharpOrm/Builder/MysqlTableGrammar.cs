using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class MysqlTableGrammar : TableGrammar
    {
        public MysqlTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        public override SqlExpression Exists()
        {
            return new SqlExpression("CALL sys.table_exists(DATABASE(), ?, @`table_type`); SELECT @`table_type` = ?;", this.Name.Name, this.Schema.Temporary ? "TEMPORARY" : "BASE TABLE");
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

        private void WriteUnique(QueryConstructor query)
        {
            if (this.Schema.Columns.PrimaryKeys.Length == 0)
                return;

            query.AddFormat(",CONSTRAINT UC_{0} UNIQUE (", this.Name).AddJoin(",", this.Schema.Columns.PrimaryKeys.Select(x => x.ColumnName)).Add(')');
        }

        private void WritePk(QueryConstructor query)
        {
            var uniques = this.Schema.Columns.Where(x => x.Unique).ToArray();
            if (uniques.Length == 0)
                return;

            query.AddFormat(",CONSTRAINT PK_{0} PRIMARY KEY (", this.Name).AddJoin(",", uniques.Select(x => x.ColumnName)).Add(')');
        }

        private SqlExpression CreateBased()
        {
            QueryConstructor query = this.GetCreateTableQuery();
            query.Add(new MysqlGrammar(this.Schema.BasedQuery).Select());
            return query.ToExpression();
        }

        private QueryConstructor GetCreateTableQuery()
        {
            QueryConstructor query = this.GetConstructor();
            query.Add("CREATE ");

            if (this.Schema.Temporary)
                query.Add("TEMPORARY ");

            query.AddFormat("TABLE {0} ", this.Name);

            return query;
        }

        public override SqlExpression Drop()
        {
            var query = this.GetConstructor();
            query.Add("DROP ");

            if (this.Schema.Temporary)
                query.Add("TEMPORARY ");

            query.Add("TABLE ").Add(this.Name.Name);

            return query.ToExpression();
        }

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException("The column name cannot contain \".\"");

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetMySqlDataType(column);
            string autoIncrement = column.AutoIncrement ? "AUTO_INCREMENT" : "";
            string nullable = column.AllowDBNull ? "DEFAULT NULL" : "NOT NULL";

            return $"{columnName} {dataType} {nullable} {autoIncrement}";
        }

        //Ref: https://medium.com/dbconvert/mysql-and-sql-servers-data-types-mapping-4cedc95de638
        private string GetMySqlDataType(DataColumn column)
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
                return $"VARCHAR({this.GetGuidSize()})";

            throw new ArgumentException($"Unsupported data type: {dataType.Name}");
        }

        private string GetStringType(int maxSize)
        {
            if (maxSize <= 0)
                maxSize = 65535;

            if (maxSize == 255)
                return "TINYTEXT";

            if (maxSize <= 16383)
                return $"VARCHAR({maxSize})";

            if (maxSize <= 65635)
                return "TEXT";

            if (maxSize <= 16777215)
                return "MEDIUMTEXT";

            return "LONGTEXT";
        }

        /// <summary>
        /// Ref: https://learn.microsoft.com/pt-br/dotnet/api/system.guid.tostring?view=net-8.0
        /// </summary>
        /// <returns></returns>
        private int GetGuidSize()
        {
            string format = this.Config.Translation.GuidFormat;
            switch (format)
            {
                case "N": return 32;
                case "D": return 36;
                case "B":
                case "P": return 38;
                default: return 68;
            }
        }
    }
}
