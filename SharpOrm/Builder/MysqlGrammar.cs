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

        protected override void ConfigureInsertQuery(QueryBase query, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.builder.AddAndReplace(
                query.ToString(),
                '?',
                (count) => this.builder.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        protected override void ConfigureInsertExpression(SqlExpression expression, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.builder.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => this.builder.AddParameter(expression.Parameters[count - 1])
            );
        }

        protected override void ConfigureBulkInsert(IEnumerable<Row> rows)
        {
            using (var @enum = rows.GetEnumerator())
            {
                if (!@enum.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.ConfigureInsert(@enum.Current.Cells, false);

                while (@enum.MoveNext())
                {
                    this.builder.Add(", ");
                    this.AppendInsertCells(@enum.Current.Cells);
                }
            }
        }

        protected override void ConfigureDelete()
        {
            this.ThrowOffsetNotSupported();

            this.builder.Add("DELETE");
            this.ApplyDeleteJoins();
            this.builder.Add(" FROM ").Add(this.Info.TableName.GetName(true, this.Info.Config));

            this.ApplyJoins();
            this.WriteWhere(true);

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.AddLimit();
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            this.AppendInsertHeader(cells.Select(c => c.Name));
            this.builder.Add("VALUES ");
            this.AppendInsertCells(cells);

            if (this.Query.InsertReturnId && getGeneratedId && this.Query.ReturnsInsetionId)
                this.builder.Add("; SELECT LAST_INSERT_ID();");
        }

        protected void AppendInsertHeader(IEnumerable<string> columns)
        {
            this.builder
               .Add("INSERT INTO ")
               .Add(this.GetTableName(false))
               .Add(" (")
               .AddJoin(", ", columns.Select(this.Info.Config.ApplyNomenclature))
               .Add(") ");
        }

        protected void AppendInsertCells(IEnumerable<Cell> cells)
        {
            this.builder.Add('(');
            this.AppendCells(cells);
            this.builder.Add(")");
        }

        protected override void ConfigureCount(Column column)
        {
            bool safeDistinct = (column == null || column.IsAll()) && this.Query.Distinct;

            if (safeDistinct)
                this.builder.Add("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, safeDistinct ? null : column);

            if (safeDistinct)
                this.builder.Add(") ").Add(this.Info.Config.ApplyNomenclature("count"));
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn)
        {
            bool isCount = countColumn != null;
            bool isCustomCount = countColumn != null && countColumn.IsCount;

            this.builder.Add("SELECT ");

            if (isCount && !isCustomCount)
                this.builder.Add("COUNT(");

            if (this.Query.Distinct && !isCustomCount)
                this.builder.Add("DISTINCT ");

            if (isCount)
            {
                WriteSelect(countColumn);
                if (!isCustomCount)
                    this.builder.Add(')');
            }
            else
            {
                this.WriteSelectColumns();
            }

            this.builder.Add(" FROM ").Add(this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (isCount)
                return;

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.WritePagination();
        }

        private void WritePagination()
        {
            if (this.Query.Limit is null && this.Query.Offset is null)
                return;

            this.builder.Add(" LIMIT ").Add(this.Query.Limit ?? int.MaxValue);

            if (this.Query.Offset != null)
                this.builder.Add(" OFFSET ").Add(this.Query.Offset);
        }

        protected bool CanWriteOrderby()
        {
            return this.Info.Select.Length != 1 || !this.Info.Select[0].IsCount;
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            this.ThrowOffsetNotSupported();

            using (var en = cells.GetEnumerator())
            {
                if (!en.MoveNext())
                    throw new InvalidOperationException(Messages.NoColumnsInserted);

                this.builder.Add("UPDATE ").Add(this.GetTableName(false));
                if (this.Info.Joins.Count > 0 && !string.IsNullOrEmpty(this.Info.TableName.Alias))
                    this.builder.Add(' ').Add(this.ApplyNomenclature(this.Info.TableName.Alias));

                this.ApplyJoins();

                this.builder.Add(" SET ");
                this.builder.AddJoin(WriteUpdateCell, ", ", en);

                this.WriteWhere(true);
            }

            this.AddLimit();
        }

        private void AddLimit()
        {
            if (this.Query.Limit != null)
                this.builder.Add(" LIMIT ").Add(this.Query.Limit);
        }

        protected void ApplyJoins()
        {
            if (this.Info.Joins.Count > 0)
                foreach (var join in this.Info.Joins)
                    this.WriteJoin(join);
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            this.builder
                .Add(' ')
                .Add(join.Type)
                .Add(" JOIN ")
                .Add(this.GetTableName(join, true))
                .Add(" ON ");

            this.WriteWhereContent(join.Info);
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (this.Info.Where.Empty)
                return;

            this.builder.Add(" WHERE ");
            if (configureParameters) this.WriteWhereContent(this.Info);
            else this.builder.Add(this.Info.Where);
        }

        protected void WriteWhereContent(QueryBaseInfo info)
        {
            this.builder.AddAndReplace(
                info.Where.ToString(),
                '?',
                (count) => this.builder.AddParameter(info.Where.Parameters[count - 1])
            );
        }

        protected void ThrowOffsetNotSupported()
        {
            if (this.Query.Offset is int val && val != 0)
                throw new NotSupportedException("Offset is not supported in this operation.");
        }
    }
}
