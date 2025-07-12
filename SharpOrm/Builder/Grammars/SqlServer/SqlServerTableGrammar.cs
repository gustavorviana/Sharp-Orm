using SharpOrm.Builder.Grammars.SqlServer.Builder;
using SharpOrm.Builder.Grammars.SqlServer.ColumnTypes;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Msg;
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
        protected override IIndexSqlBuilder IndexBuilder { get; } = new SqlServerIndexBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public SqlServerTableGrammar(QueryConfig config, ITableSchema schema) : base(config, schema)
        {
            ColumnTypes.Add(new ColumnType(typeof(int), "INT"));
            ColumnTypes.Add(new ColumnType(typeof(long), "BIGINT"));
            ColumnTypes.Add(new ColumnType(typeof(short), "SMALLINT"));
            ColumnTypes.Add(new ColumnType(typeof(byte), "TINYINT"));
            ColumnTypes.Add(new ColumnType(typeof(float), "REAL"));
            ColumnTypes.Add(new ColumnType(typeof(double), "FLOAT"));
            ColumnTypes.Add(new ColumnType(typeof(decimal), "DECIMAL"));
            ColumnTypes.Add(new ColumnType(typeof(bool), "BIT"));
            ColumnTypes.Add(new ColumnType(typeof(char), "NCHAR(1)"));
            ColumnTypes.Add(new ColumnType(typeof(DateTime), "DATETIME"));
            ColumnTypes.Add(new ColumnType(typeof(TimeSpan), "TIME"));
            ColumnTypes.Add(new ColumnType(typeof(byte[]), "VARBINARY(MAX)"));
            ColumnTypes.Add(new ColumnType(typeof(MemoryStream), "VARBINARY(MAX)"));
            ColumnTypes.Add(new ColumnType(typeof(Guid), "UNIQUEIDENTIFIER"));
            ColumnTypes.Add(new SqlServerStringColumnTypeMap(false));

            ConstraintBuilders.Add(new SqlServerPrimaryKeyConstraintBuilder());
            ConstraintBuilders.Add(new SqlServerForeignKeyConstraintBuilder());
            ConstraintBuilders.Add(new SqlServerUniqueConstraintBuilder());
            ConstraintBuilders.Add(new SqlServerCheckConstraintBuilder());
        }

        protected override DbName LoadName()
        {
            bool isTempName = Schema.Name.StartsWith("#");
            if (isTempName && !Schema.Temporary)
                throw new InvalidOperationException(string.Format(Messages.Query.FirstCharInvalid, "#"));

            if (Schema.Name.EndsWith("_"))
                throw new NotSupportedException(string.Format(Messages.Query.FirstCharInvalid, "_"));

            if (!Schema.Temporary)
                return new DbName(Schema.Name, string.Empty);

            if (Schema.Name.Contains("."))
                throw new NotSupportedException(Messages.SqlServer.InvalidTempTableName);

            if (Schema.Name.Length > 115)
                throw new InvalidOperationException(Messages.SqlServer.SchemaNameOverflow);

            if (isTempName) return new DbName(Schema.Name, string.Empty, false);

            return new DbName(string.Concat("#", Schema.Name), string.Empty);
        }

        public override SqlExpression Create()
        {
            if (BasedQuery != null)
                return CreateBased();

            var query = GetBuilder()
                .AddFormat("CREATE TABLE [{0}] (", Name.Name)
                .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));

            WriteConstraints(query);

            return query.Add(')').ToExpression();
        }

        private SqlExpression GetColumnDefinition(DataColumn column)
        {
            return new SqlServerColumnBuilder(Config, GetColumnType(column), column).Build();
        }

        private SqlExpression CreateBased()
        {
            QueryBuilder query = GetBuilder();
            query.Add("SELECT ");

            if (BasedQuery.Limit is int limit && BasedQuery.Offset is null)
                query.Add($"TOP(").Add(limit).Add(") ");

            WriteColumns(query, BasedTable.Select);

            query.Add(" INTO [").Add(Name).Add("]");

            var qGrammar = new SqlServerSelectGrammar(BasedQuery);
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
