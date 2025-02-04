using System;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    internal class SqlServerGrammarBase : GrammarBase
    {
        public SqlServerGrammarBase(GrammarBase owner) : base(owner)
        {
        }

        protected void AddLimit()
        {
            if (Query.Limit is int limit && limit >= 0)
                builder.Add(" TOP(").Add(limit).Add(')');
        }

        protected override void WriteTable(QueryBase query)
        {
            base.WriteTable(query);
            WriteGrammarOptions(query);
        }

        protected void WriteGrammarOptions(QueryBase query)
        {
            SqlServerGrammarOptions.WriteTo(builder, query);
        }

        protected void ValidateOffsetOrderBy()
        {
            if (Info.Orders.Length == 0)
                throw new InvalidOperationException($"You cannot use {nameof(Query)}.{nameof(Query.Offset)} without defining a sort column.");
        }
    }
}
