using SharpOrm.Builder.Grammars.Interfaces;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.SqlServer
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

        protected override IDeleteGrammar GetDeleteGrammar()
            => new SqlServerDeleteGrammar(Query);

        protected override IUpdateGrammar GetUpdateGrammar()
            => new SqlServerUpdateGrammar(Query);

        protected override ISelectGrammar GetSelectGrammar()
            => new SqlServerSelectGrammar(Query);

        protected override IInsertGrammar GetInsertGrammar()
            => new SqlServerInsertGrammar(Query);

        protected override IUpsertGrammar GetUpsertGrammar()
            => new SqlServerUpsertGrammar(Query);

        protected override IBulkInsertGrammar GetBulkInsertGrammar()
            => new SqlServerBulkInsertGrammar(Query);
    }
}
