using SharpOrm.Builder.Grammars.Mysql;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to SQLite.
    /// </summary>
    public class SqliteGrammar : MysqlGrammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteGrammar"/> class with the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public SqliteGrammar(Query query) : base(query)
        {
            builder.paramInterceptor += (original) =>
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

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            ThrowNotSupportedOperations("INSERT");
            base.ConfigureInsert(cells, false);

            if (getGeneratedId && Query.ReturnsInsetionId)
                builder.Add("; SELECT last_insert_rowid();");
        }

        protected override void ConfigureDelete()
        {
            ThrowNotSupportedOperations("DELETE");
            ValidateAlias();

            base.ConfigureDelete();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            ThrowNotSupportedOperations("UPDATE");

            ValidateAlias();
            base.ConfigureUpdate(cells);
        }

        private void ValidateAlias()
        {
            if (!string.IsNullOrEmpty(Info.TableName.Alias))
                throw new NotSupportedException("SQLite does not support executing a DELETE with a table alias.");
        }

        private void ThrowNotSupportedOperations(string operationName)
        {
            if (Query.Limit > 0)
                throw new NotSupportedException($"SQLite does not support `Limit` with `{operationName}`.");

            if (Query.Offset > 0)
                throw new NotSupportedException($"SQLite does not support `Offset` with `{operationName}`.");

            if (Info.Joins.Count > 0)
                throw new NotSupportedException($"SQLite does not support `Joins` with `{operationName}`.");

            if (!Info.Having.Empty)
                throw new NotSupportedException($"SQLite does not support `Having` with `{operationName}`.");

            if (Info.Orders.Length > 0)
                throw new NotSupportedException($"SQLite does not support `Orders` with `{operationName}`.");
        }
    }
}
