﻿using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    public class SqliteGrammar : MysqlGrammar
    {
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

            if (getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT last_insert_rowid();");
        }

        protected override void ConfigureDelete()
        {
            if (this.Info.Joins.Count > 0)
                throw new NotSupportedException("SQLite does not support DELETE with JOIN.");

            base.ConfigureDelete();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            if (this.Info.Joins.Count > 0)
                throw new NotSupportedException("SQLite does not support UPDATE with JOIN.");

            base.ConfigureUpdate(cells);
        }
    }
}