using SharpOrm.Builder.DataTranslation;
using System;
using System.Data;
using System.Linq;

namespace SharpOrm.Builder
{
    public class SqliteTableGrammar : TableGrammar
    {
        public SqliteTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        protected override DbName LoadName()
        {
            if (this.Schema.Temporary)
                return new DbName(string.Concat("temp_", this.Schema.Name), "");

            return new DbName(this.Schema.Name, "");
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

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException("The column name cannot contain \".\"");

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetSqliteDataType(column);
            string nullable = column.AllowDBNull ? " NULL" : " NOT NULL";

            return string.Concat(columnName, " ", dataType, nullable);
        }

        private string GetSqliteDataType(DataColumn column)
        {
            if (this.GetCustomColumnTypeMap(column) is ColumnTypeMap map)
                return map.GetTypeString(column);

            var dataType = column.DataType;

            if (dataType == typeof(long))
                return "NUMERIC";

            if (TranslationUtils.IsNumberWithoutDecimal(dataType) || dataType == typeof(bool))
                return "INTEGER";

            if (TranslationUtils.IsNumberWithDecimal(dataType))
                return "REAL";

            if (dataType == typeof(string))
                return column.MaxLength < 1 ? "TEXT" : string.Concat("TEXT(", column.MaxLength, ")");

            if (dataType == typeof(char))
                return "TEXT(1)";

            if (dataType == typeof(DateTime))
                return "TEXT(19)";

            if (dataType == typeof(TimeSpan))
                return "TEXT(8)";

            if (dataType == typeof(byte[]))
                return "BLOB";

            if (dataType == typeof(Guid))
                return string.Concat("TEXT(", this.GetGuidSize(), ")");

            throw new ArgumentException($"Unsupported data type: {dataType.Name}");
        }

        protected override void WritePk(QueryBuilder query)
        {
            var pks = this.GetPrimaryKeys().OrderBy(x => x.AutoIncrement).ToArray();
            if (pks.Length == 0)
                return;

            if (pks.Count(x => x.AutoIncrement) > 1)
                throw new NotSupportedException("There is no support for more than one column with autoincrement.");

            query.Add(",PRIMARY KEY (").AddJoin(",", pks.Select(BuildAutoIncrement)).Add(')');
        }

        private string BuildAutoIncrement(DataColumn column)
        {
            string name = this.Config.ApplyNomenclature(column.ColumnName);
            return column.AutoIncrement ? string.Concat(name, " AUTOINCREMENT") : name;
        }

        private SqlExpression CreateBased()
        {
            return this.GetCreateTableQuery()
                .Add(" AS ")
                .Add(new SqliteGrammar(this.Schema.BasedQuery).Select())
                .ToExpression();
        }

        private QueryBuilder GetCreateTableQuery()
        {
            QueryBuilder query = this.GetBuilder();
            query.Add("CREATE TABLE ");

            if (this.Schema.Temporary)
                query.Add("temp.");

            return query.Add(this.ApplyNomenclature(this.Name.Name));
        }

        public override SqlExpression Drop()
        {
            return this.GetBuilder()
                .Add("DROP ")
                .Add("TABLE ")
                .Add(this.ApplyNomenclature(this.Name.Name))
                .ToExpression();
        }

        public override SqlExpression Exists()
        {
            string table = this.Schema.Temporary ? "sqlite_temp_master" : "sqlite_master";
            return new SqlExpression($"SELECT COUNT(*) FROM {table} WHERE type='table' AND name = \"{this.Name.Name}\";");
        }
    }
}
