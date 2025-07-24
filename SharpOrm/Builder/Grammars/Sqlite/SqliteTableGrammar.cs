using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Sqlite.Builder;
using SharpOrm.Builder.Grammars.Sqlite.ColumnTypes;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQLite.
    /// </summary>
    public class SqliteTableGrammar : TableGrammar
    {
        protected override IIndexSqlBuilder IndexBuilder { get; } = new SqliteIndexBuilder();
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public SqliteTableGrammar(QueryConfig config, ITableSchema schema) : base(config, schema)
        {
            ColumnTypes.Add(new ColumnType(typeof(long), "NUMERIC"));
            ColumnTypes.Add(new ColumnType(typeof(char), "TEXT(1)"));
            ColumnTypes.Add(new ColumnType(typeof(DateTime), "TEXT(19)"));
            ColumnTypes.Add(new ColumnType(typeof(TimeSpan), "TEXT(8)"));
            ColumnTypes.Add(new ColumnType(typeof(byte[]), "BLOB"));
            ColumnTypes.Add(new GuidColumnType(config.Translation, "TEXT"));
            ColumnTypes.Add(new SqliteNumberWithoutDecimalColumnTypeMap());
            ColumnTypes.Add(new SqliteNumberWithDecimalColumnTypeMap());
            ColumnTypes.Add(new SqliteStringColumnTypeMap());

            ConstraintBuilders.Add(new SqlitePrimaryKeyConstraintBuilder());
            ConstraintBuilders.Add(new SqliteForeignKeyConstraintBuilder());
            ConstraintBuilders.Add(new SqliteUniqueConstraintBuilder());
            ConstraintBuilders.Add(new SqliteCheckConstraintBuilder());
        }

        protected override DbName LoadName()
        {
            if (Schema.Temporary && !Schema.Name.StartsWith("temp_"))
                return new DbName(string.Concat("temp_", Schema.Name), string.Empty);

            return new DbName(Schema.Name, string.Empty);
        }

        public override SqlExpression Create()
        {
            if (BasedQuery != null)
                return CreateBased();

            if (Schema.Constraints.OfType<PrimaryKeyConstraint>().Count() > 1 && Schema.Columns.Count(x => x.AutoIncrement) > 0)
                throw new InvalidOperationException(Messages.Sqlite.MultiplePrimaryKeyWithAutoIncrementError);

            var query = GetCreateTableQuery()
                 .Add('(')
                 .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));

            foreach (var item in Schema.Constraints.Where(x => !(x is PrimaryKeyConstraint pk) || (!pk.AutoIncrement && pk.Columns.Length > 1)))
                query.Add(',').Add(BuildConstraint(item));

            return query.Add(')').ToExpression();
        }

        private SqlExpression GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException(Messages.Query.ColumnNotSuportDot);

            var pk = Schema.Constraints.OfType<PrimaryKeyConstraint>().FirstOrDefault(x => x.Columns.Contains(column.ColumnName));

            var builder = new QueryBuilder();
            builder
                .Add(Config.ApplyNomenclature(column.ColumnName))
                .Add(' ')
                .Add(GetColumnType(column))
                .Add(' ');

            if (pk != null && pk.Columns.Length == 1)
                builder.Add(BuildConstraint(pk)).Add(' ');

            builder.Add(column.AllowDBNull ? "NULL" : "NOT NULL");

            return builder.ToExpression();
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
                .Add(new SqliteGrammar(BasedQuery).Select())
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
