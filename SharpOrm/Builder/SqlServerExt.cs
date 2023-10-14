using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    public static class SqlServerExt
    {
        public static void EnableNoLock(this Query query)
        {
            query.GrammarOptions = new SqlServerGrammarOptions
            {
                NoLock = true
            };
        }

        public static bool IsNoLock(this Query query)
        {
            return query.GrammarOptions is SqlServerGrammarOptions opt && opt.NoLock;
        }
    }
}
