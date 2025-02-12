using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public abstract class InsertGrammar : InsertBaseGrammar, IInsertGrammar
    {
        protected InsertGrammar(Query query, bool useLotQueryBuilder) : base(query, useLotQueryBuilder)
        {
        }

        public InsertGrammar(Query query) : base(query)
        {
        }

        public virtual void Build(SqlExpression expression, string[] columnNames)
        {
            AppendInsertHeader(columnNames);
            Builder.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => Builder.AddParameter(expression.Parameters[count - 1])
            );
        }

        public abstract void Build(IEnumerable<Cell> cells, bool getGeneratedId);
    }
}
