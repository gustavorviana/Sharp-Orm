﻿namespace SharpOrm.Builder.Grammars.Mysql
{
    public abstract class MysqlGrammarBase : GrammarBase
    {
        public MysqlGrammarBase(GrammarBase grammar) : base(grammar)
        {
        }

        protected void AddLimit()
        {
            if (Query.Limit != null)
                Builder.Add(" LIMIT ").Add(Query.Limit);
        }
    }
}
