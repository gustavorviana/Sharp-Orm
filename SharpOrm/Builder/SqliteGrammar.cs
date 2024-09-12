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
                    return date.ToString("s");

                if (original is DateTimeOffset offset)
                    return ToDateTime(offset).ToString("s");

                return original;
            };
        }

        private DateTime ToDateTime(DateTimeOffset offset)
        {
            return this.Info.Config.Translation.ConvertDate(offset.UtcDateTime, true);
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            base.ConfigureInsert(cells, false);

            if (this.Query.InsertReturnId && getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT last_insert_rowid();");
        }

        protected override void ConfigureDelete()
        {
            if (this.Info.Joins.Count > 0)
                throw new NotSupportedException("SQLite does not support DELETE with JOIN.");

            this.ValidateAlias();
            base.ConfigureDelete();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            if (this.Info.Joins.Count > 0)
                throw new NotSupportedException("SQLite does not support UPDATE with JOIN.");

            this.ValidateAlias();
            base.ConfigureUpdate(cells);
        }

        private void ValidateAlias()
        {
            if (!string.IsNullOrEmpty(this.Info.TableName.Alias))
                throw new NotSupportedException("SQLite does not support executing a DELETE with a table alias.");
        }
    }
}
