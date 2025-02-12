using SharpOrm.Builder.Grammars.Interfaces;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Mysql
{
    /// <summary>
    /// Provides the implementation for building SQL queries specific to MySQL using a fluent interface.
    /// </summary>
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
            ParamInterceptor += (original) =>
            {
                if (original is DateTimeOffset offset)
                    return TimeZoneInfo.ConvertTime(offset.UtcDateTime, GetTimeZoneInfo());

                return original;
            };
        }

        private TimeZoneInfo GetTimeZoneInfo()
        {
            return Info.Config.Translation.DbTimeZone;
        }

        protected override IInsertGrammar GetInsertGrammar()
            => new MysqlInsertGrammar(Query);

        protected override IDeleteGrammar GetDeleteGrammar()
            => new MysqlDeleteGrammar(Query);

        protected override IUpdateGrammar GetUpdateGrammar()
            => new MysqlUpdateGrammar(Query);

        protected override ISelectGrammar GetSelectGrammar()
            => new MysqlSelectGrammar(Query);

        protected override IUpsertGrammar GetUpsertGrammar()
            => new MysqlUpsertGrammar(Query);

        protected override IBulkInsertGrammar GetBulkInsertGrammar()
            => new BulkInsertGrammar(Query);
    }
}
