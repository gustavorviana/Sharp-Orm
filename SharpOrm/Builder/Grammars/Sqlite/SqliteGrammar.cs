using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Builder.Grammars.Mysql;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQLite.
    /// </summary>
    public class SqliteGrammar : Grammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteGrammar"/> class with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public SqliteGrammar(Query query) : base(query)
        {
            ParamInterceptor += (original) =>
            {
                if (original is DateTime date)
                    return date.ToString(DateTranslation.Format);

                if (original is DateTimeOffset offset)
                    return TimeZoneInfo.ConvertTime(offset.UtcDateTime, GetTimeZoneInfo()).ToString(DateTranslation.Format);

                return original;
            };
        }

        private TimeZoneInfo GetTimeZoneInfo()
        {
            return Info.Config.Translation.DbTimeZone;
        }

        protected override IInsertGrammar GetInsertGrammar()
            => new SqliteInsertGrammar(Query);

        protected override ISelectGrammar GetSelectGrammar()
            => new MysqlSelectGrammar(Query);

        protected override IDeleteGrammar GetDeleteGrammar()
        {
            ThrowNotSupportedOperations(Query, "DELETE");
            ValidateAlias();

            return new SqLiteDeleteGrammar(Query);
        }

        protected override IUpdateGrammar GetUpdateGrammar()
        {
            ThrowNotSupportedOperations(Query, "UPDATE");

            ValidateAlias();
            return new MysqlUpdateGrammar(Query);
        }

        protected override IUpsertGrammar GetUpsertGrammar()
            => new SqliteUpsertGrammar(Query);

        private void ValidateAlias()
        {
            if (!string.IsNullOrEmpty(Info.TableName.Alias))
                throw new NotSupportedException("SQLite does not support executing a DELETE with a table alias.");
        }

        internal static void ThrowNotSupportedOperations(Query query, string operationName)
        {
            if (query.Limit > 0)
                throw new NotSupportedException($"SQLite does not support `Limit` with `{operationName}`.");

            if (query.Offset > 0)
                throw new NotSupportedException($"SQLite does not support `Offset` with `{operationName}`.");

            if (query.Info.Joins.Count > 0)
                throw new NotSupportedException($"SQLite does not support `Joins` with `{operationName}`.");

            if (!query.Info.Having.Empty)
                throw new NotSupportedException($"SQLite does not support `Having` with `{operationName}`.");

            if (query.Info.Orders.Length > 0)
                throw new NotSupportedException($"SQLite does not support `Orders` with `{operationName}`.");
        }

        protected override IBulkInsertGrammar GetBulkInsertGrammar()
            => new BulkInsertGrammar(Query);

        private class SqLiteDeleteGrammar : MysqlDeleteGrammar
        {
            public SqLiteDeleteGrammar(Query query) : base(query)
            {
            }

            public override void BuildIncludingJoins(DbName[] joinNames)
            {
                throw new NotSupportedException($"SQLite does not support `DELETE` with `Joins`.");
            }
        }
    }
}
