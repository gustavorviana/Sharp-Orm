using SharpOrm.Builder.Grammars.Mysql;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;

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
            new InsertGrammar(this).BuildInsert(cells);

            if (getGeneratedId && Query.ReturnsInsetionId)
                builder.Add("; SELECT last_insert_rowid();");
        }

        protected override void ConfigureDelete()
        {
            ThrowNotSupportedOperations("DELETE");
            ValidateAlias();

            new MysqlDeleteGrammar(this).Build();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            ThrowNotSupportedOperations("UPDATE");

            ValidateAlias();
            new MysqlUpdateGrammar(this).Build(cells); ;
        }

        protected override void ConfigureCount(Column column)
        {
            new MysqlSelectGrammar(this).BuildCount(column);
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            new MysqlSelectGrammar(this).BuildSelect(configureWhereParams);
        }

        private void ValidateAlias()
        {
            if (!string.IsNullOrEmpty(Info.TableName.Alias))
                throw new NotSupportedException("SQLite does not support executing a DELETE with a table alias.");
        }

        protected override void ConfigureUpsert(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            builder.AddFormat("INSERT INTO {0} (", Info.Config.ApplyNomenclature(target.TableName.Name));
            WriteColumns("", insertColumns);
            builder.Add(')');

            builder.Add(" SELECT ");
            WriteColumns(source.Alias + '.', insertColumns);

            builder.AddFormat(" FROM {0}", source.GetFullName());
            builder.Add(" WHERE true ON CONFLICT(");
            WriteColumns("", whereColumns);
            builder.Add(") SET ");

            WriteColumn(source.Alias, updateColumns[0]);

            for (int i = 1; i < updateColumns.Length; i++)
            {
                builder.Add(", ");
                WriteColumn(source.Alias, updateColumns[i]);
            }
        }

        private void WriteColumn(string srcAlias, string column)
        {
            column = Info.Config.ApplyNomenclature(column);
            builder.AddFormat("{1}={0}.{1}", srcAlias, column);
        }

        private void WriteColumns(string prefix, string[] columns)
        {
            var column = Info.Config.ApplyNomenclature(columns[0]);
            builder.AddFormat("{0}{1}", prefix, column);

            for (int i = 1; i < columns.Length; i++)
            {
                builder.Add(", ");
                column = Info.Config.ApplyNomenclature(columns[i]);

                builder.AddFormat("{0}{1}", prefix, column);
            }
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
