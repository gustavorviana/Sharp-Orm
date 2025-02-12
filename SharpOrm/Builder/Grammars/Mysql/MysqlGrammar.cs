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
            Builder.paramInterceptor += (original) =>
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

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            new InsertGrammar(this).BuildInsert(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                Builder.Add("; SELECT LAST_INSERT_ID();");
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

        protected override void ConfigureUpsert(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            new MysqlUpsertGrammar(this).Build(target, source, whereColumns, updateColumns, insertColumns);
        }

        protected override void ConfigureUpsert(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            new MysqlUpsertGrammar(this).Build(target, rows, whereColumns, updateColumns);
        }
    }
}
