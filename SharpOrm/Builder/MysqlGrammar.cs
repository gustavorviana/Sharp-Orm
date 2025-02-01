using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Sgbd.Mysql;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the implementation for building SQL queries specific to MySQL using a fluent interface.
    /// </summary>
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
            this.builder.paramInterceptor += (original) =>
            {
                if (original is DateTimeOffset offset)
                    return TimeZoneInfo.ConvertTime(offset.UtcDateTime, GetTimeZoneInfo());

                return original;
            };
        }

        private TimeZoneInfo GetTimeZoneInfo()
        {
            return this.Info.Config.Translation.DbTimeZone;
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            new InsertGrammar(this).BuildInsert(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                builder.Add("; SELECT LAST_INSERT_ID();");
        }

        protected override void ConfigureDelete()
        {
            new MysqlDeleteGrammar(this).Build();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            new MysqlUpdateGrammar(this).Build(cells);
        }

        protected override void ConfigureCount(Column column)
        {
            new MysqlSelectGrammar(this).BuildCount(column);
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
           new MysqlSelectGrammar(this).BuildSelect(configureWhereParams);
        }
    }
}
