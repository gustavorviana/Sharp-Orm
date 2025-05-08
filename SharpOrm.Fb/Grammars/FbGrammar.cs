using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Interfaces;
using System;

namespace SharpOrm.Fb.Grammars
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQL Server.
    /// </summary>
    public class FbGrammar : Grammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FbGrammar"/> class with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public FbGrammar(Query query) : base(query)
        {
        }

        protected override IDeleteGrammar GetDeleteGrammar()
            => new FbDeleteGrammar(Query);

        protected override IUpdateGrammar GetUpdateGrammar()
            => new FbUpdateGrammar(Query);

        protected override ISelectGrammar GetSelectGrammar()
            => new FbSelectGrammar(Query);

        protected override IInsertGrammar GetInsertGrammar()
            => new FbInsertGrammar(Query);

        protected override IUpsertGrammar GetUpsertGrammar()
            => throw new NotSupportedException();

        protected override IBulkInsertGrammar GetBulkInsertGrammar()
            => new FbBulkInsertGrammar(Query);
    }
}
