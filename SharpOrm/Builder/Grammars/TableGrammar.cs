using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.Msg;
using System;
using System.Data;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    /// <summary>
    /// Provides the base implementation for building SQL table-related commands using a fluent interface.
    /// </summary>
    public abstract class TableGrammar
    {
        /// <summary>
        /// Gets the read-only query information.
        /// </summary>
        protected readonly IReadonlyQueryInfo queryInfo;

        protected ColumnTypeProvider ColumnTypes { get; set; } = new ColumnTypeProvider();
        protected ItemsProvider<ConstraintBuilder> ConstraintBuilders { get; set; } = new ItemsProvider<ConstraintBuilder>();


        /// <summary>
        /// Gets the table schema.
        /// </summary>
        public TableSchema Schema { get; }


        /// <summary>
        /// Gets the query configuration.
        /// </summary>
        public QueryConfig Config => queryInfo.Config;

        /// <summary>
        /// Database name in the database's standard format.
        /// </summary>
        public virtual DbName Name { get; }

        /// <summary>
        /// Gets the query information for the base table.
        /// </summary>
        protected QueryInfo BasedTable => Schema.BasedQuery.Info;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public TableGrammar(QueryConfig config, TableSchema schema)
        {
            Schema = schema;
            Name = LoadName();
            queryInfo = new ReadonlyQueryInfo(config, Name);
        }

        /// <summary>
        /// Loads the name of the database table.
        /// </summary>
        /// <returns>The name of the database table.</returns>
        protected virtual DbName LoadName()
        {
            return new DbName(Schema.Name);
        }

        /// <summary>
        /// Creates a SQL expression for creating the table.
        /// </summary>
        /// <returns>The SQL expression for creating the table.</returns>
        public abstract SqlExpression Create();

        /// <summary>
        /// Writes the columns to the query.
        /// </summary>
        /// <param name="query">The query builder.</param>
        /// <param name="columns">The columns to write.</param>
        protected void WriteColumns(QueryBuilder query, Column[] columns)
        {
            if (columns.Length == 0)
            {
                query.Add("*");
                return;
            }

            query.AddExpression(columns[0]);

            for (int i = 1; i < columns.Length; i++)
                query.Add(",").AddExpression(columns[i]);
        }

        /// <summary>
        /// Creates a SQL expression for dropping the table.
        /// </summary>
        /// <returns>The SQL expression for dropping the table.</returns>
        public virtual SqlExpression Drop()
        {
            return new SqlExpression(string.Concat("DROP TABLE ", Config.ApplyNomenclature(Name.Name)));
        }

        /// <summary>
        /// Creates a SQL expression for checking if the table exists.
        /// </summary>
        /// <returns>The SQL expression for checking if the table exists.</returns>
        public abstract SqlExpression Exists();

        /// <summary>
        /// Creates a SQL expression for truncating the table.
        /// </summary>
        /// <returns>The SQL expression for truncating the table.</returns>
        public virtual SqlExpression Truncate()
        {
            return new SqlExpression(string.Concat("TRUNCATE TABLE ", ApplyNomenclature(Name.Name)));
        }

        protected string GetColumnType(DataColumn column)
        {
            if (column.ExtendedProperties.TryGet(ExtendedPropertyKeys.ColumnType, out var type) && type is string strType)
                return strType;

            if (GetCustomColumnTypeMap(column) is IColumnTypeMap map)
                return map.Build(column);

            return ColumnTypes.BuildType(column) ??
                throw new ArgumentException(string.Format(Messages.Table.UnsupportedType, column.DataType.Name));
        }

        /// <summary>
        /// Gets the custom column type map for the specified column.
        /// </summary>
        /// <param name="column">The data column.</param>
        /// <returns>The custom column type map.</returns>
        protected IColumnTypeMap GetCustomColumnTypeMap(DataColumn column)
        {
            return queryInfo.Config.CustomColumnTypes.Get(column);
        }

        /// <summary>
        /// Writes the primary key constraint to the query.
        /// </summary>
        /// <param name="query">The query builder.</param>
        protected virtual void WritePk(QueryBuilder query)
        {
            var pks = GetPrimaryKeys();
            if (pks.Length != 0)
                query.AddFormat(",CONSTRAINT {0} PRIMARY KEY (", Config.ApplyNomenclature(string.Concat("PK_", Name))).AddJoin(",", pks.Select(x => Config.ApplyNomenclature(x.ColumnName))).Add(')');
        }

        /// <summary>
        /// Gets the primary key columns.
        /// </summary>
        /// <returns>The primary key columns.</returns>
        protected DataColumn[] GetPrimaryKeys()
        {
            return Schema.Columns.PrimaryKeys;
        }

        /// <summary>
        /// Writes the unique constraint to the query.
        /// </summary>
        /// <param name="query">The query builder.</param>
        protected void WriteUnique(QueryBuilder query)
        {
            var uniques = GetUniqueKeys();
            if (uniques.Length != 0)
                query.AddFormat(",CONSTRAINT {0} UNIQUE (", Config.ApplyNomenclature(string.Concat("UC_", Name))).AddJoin(",", uniques.Select(x => Config.ApplyNomenclature(x.ColumnName))).Add(')');
        }

        /// <summary>
        /// Gets the unique key columns.
        /// </summary>
        /// <returns>The unique key columns.</returns>
        protected DataColumn[] GetUniqueKeys()
        {
            return Schema.Columns.Where(x => x.Unique).ToArray();
        }

        /// <summary>
        /// Gets a new instance of the query builder.
        /// </summary>
        /// <returns>A new instance of the query builder.</returns>
        protected QueryBuilder GetBuilder()
        {
            return new QueryBuilder(queryInfo);
        }

        /// <summary>
        /// Applies the nomenclature to the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name with the applied nomenclature.</returns>
        protected string ApplyNomenclature(string name)
        {
            return queryInfo.Config.ApplyNomenclature(name);
        }
    }
}
