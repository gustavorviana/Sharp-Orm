using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
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
            this.builder.paramInterceptor += (original) =>
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
            return this.Info.Config.Translation.DbTimeZone;
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            this.ThrowNotSupportedOperations("INSERT");
            base.ConfigureInsert(cells, false);

            if (this.Query.InsertReturnId && getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT last_insert_rowid();");
        }

        protected override void ConfigureDelete()
        {
            this.ThrowNotSupportedOperations("DELETE");
            this.ValidateAlias();

            base.ConfigureDelete();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.ThrowNotSupportedOperations("UPDATE");

            this.ValidateAlias();
            base.ConfigureUpdate(cells);
        }

        private void ValidateAlias()
        {
            if (!string.IsNullOrEmpty(this.Info.TableName.Alias))
                throw new NotSupportedException("SQLite does not support executing a DELETE with a table alias.");
        }

        private void ThrowNotSupportedOperations(string operationName)
        {
            if (this.Query.Limit > 0)
                throw new NotSupportedException($"SQLite does not support `Limit` with `{operationName}`.");

            if (this.Query.Offset > 0)
                throw new NotSupportedException($"SQLite does not support `Offset` with `{operationName}`.");

            if (this.Info.Joins.Count > 0)
                throw new NotSupportedException($"SQLite does not support `Joins` with `{operationName}`.");

            if (!this.Info.Having.Empty)
                throw new NotSupportedException($"SQLite does not support `Having` with `{operationName}`.");

            if (this.Info.Orders.Length > 0)
                throw new NotSupportedException($"SQLite does not support `Orders` with `{operationName}`.");
        }
    }
}
