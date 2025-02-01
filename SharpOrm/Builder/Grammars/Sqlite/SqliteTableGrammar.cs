using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Data;
using System.Linq;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQLite.
    /// </summary>
    public class SqliteTableGrammar : TableGrammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public SqliteTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
        }

        protected override DbName LoadName()
        {
            if (Schema.Temporary && !Schema.Name.StartsWith("temp_"))
                return new DbName(string.Concat("temp_", Schema.Name), "");

            return new DbName(Schema.Name, "");
        }

        public override SqlExpression Create()
        {
            if (Schema.BasedQuery != null)
                return CreateBased();

            if (GetPrimaryKeys().Length > 1 && Schema.Columns.Count(x => x.AutoIncrement) > 0)
                throw new InvalidOperationException("It is not possible to have more than one primary key column when there is an AUTOINCREMENT column.");

            var query = GetCreateTableQuery()
                 .Add('(')
                 .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));

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
            if (GetCustomColumnTypeMap(column) is ColumnTypeMap map)
                return map.GetTypeString(column);

            if (GetExpectedColumnType(column) is string typeColumn)
                return typeColumn;

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
                return string.Concat("TEXT(", GetGuidSize(), ")");

            throw new ArgumentException($"Unsupported data type: {dataType.Name}");
        }

        protected override void WritePk(QueryBuilder query)
        {
            var pks = GetPrimaryKeys().OrderBy(x => x.AutoIncrement).ToArray();
            if (pks.Length == 0)
                return;

            if (pks.Count(x => x.AutoIncrement) > 1)
                throw new NotSupportedException("There is no support for more than one column with autoincrement.");

            query.Add(",PRIMARY KEY (").AddJoin(",", pks.Select(BuildAutoIncrement)).Add(')');
        }

        private string BuildAutoIncrement(DataColumn column)
        {
            string name = Config.ApplyNomenclature(column.ColumnName);
            return column.AutoIncrement ? string.Concat(name, " AUTOINCREMENT") : name;
        }

        private SqlExpression CreateBased()
        {
            return GetCreateTableQuery()
                .Add(" AS ")
                .Add(new SqliteGrammar(Schema.BasedQuery).Select())
                .ToExpression();
        }

        private QueryBuilder GetCreateTableQuery()
        {
            QueryBuilder query = GetBuilder();
            query.Add("CREATE TABLE ");

            if (Schema.Temporary)
                query.Add("temp.");

            return query.Add(ApplyNomenclature(Name.Name));
        }

        public override SqlExpression Drop()
        {
            return GetBuilder()
                .Add("DROP ")
                .Add("TABLE ")
                .Add(ApplyNomenclature(Name.Name))
                .ToExpression();
        }

        public override SqlExpression Exists()
        {
            string table = Schema.Temporary ? "sqlite_temp_master" : "sqlite_master";
            return new SqlExpression($"SELECT COUNT(*) FROM {table} WHERE type='table' AND name = \"{Name.Name}\";");
        }
    }
}
