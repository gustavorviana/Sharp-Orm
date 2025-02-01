using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Sgbd.SqlServer;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQL Server.
    /// </summary>
    public class SqlServerGrammar : Grammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerGrammar"/> class with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public SqlServerGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureDelete()
        {
            new SqlServerDeleteGrammar(this).Build();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            new SqlServerUpdateGrammar(this).Build(cells);
        }

        protected override void ConfigureCount(Column column)
        {
            new SqlServerSelectGrammar(this).BuildCount(column);
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            new SqlServerSelectGrammar(this).BuildSelect(configureWhereParams);
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            new InsertGrammar(this).BuildInsert(cells);

            if (getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT SCOPE_IDENTITY();");
        }

        internal SqlExpression GetSelectFrom()
        {
            var grammar = new SqlServerSelectGrammar(this);

            this.builder.Clear();
            grammar.WriteSelectFrom(true);
            this.ApplyOrderBy();
            grammar.WritePagination();

            try
            {
                return this.builder.ToExpression();
            }
            finally
            {
                this.builder.Clear();
            }
        }
    }
}
