using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
        }

        protected override void ConfigureInsertQuery(QueryBase query, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.Constructor.AddAndReplace(
                query.ToString(),
                '?',
                (count) => this.Constructor.AddParameter(query.Info.Where.Parameters[count - 1])
            );
        }

        protected override void ConfigureInsertExpression(SqlExpression expression, IEnumerable<string> columnNames)
        {
            this.AppendInsertHeader(columnNames);
            this.Constructor.AddAndReplace(
                expression.ToString(),
                '?',
                (count) => this.Constructor.AddParameter(expression.Parameters[count - 1])
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
                    this.Constructor.Add(", ");
                    this.AppendInsertCells(@enum.Current.Cells);
                }
            }
        }

        protected override void ConfigureDelete()
        {
            this.ThrowOffsetNotSupported();

            this.Constructor.Add("DELETE");
            this.ApplyDeleteJoins();
            this.Constructor.Add(" FROM ").Add(this.Info.TableName.GetName(true, this.Info.Config));

            this.ApplyJoins();
            this.WriteWhere(true);

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.AddLimit();
        }

        protected override void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            this.AppendInsertHeader(cells.Select(c => c.Name));
            this.Constructor.Add("VALUES ");
            this.AppendInsertCells(cells);

            if (getGeneratedId && this.Query.ReturnsInsetionId)
                this.Constructor.Add("; SELECT LAST_INSERT_ID();");
        }

        protected void AppendInsertHeader(IEnumerable<string> columns)
        {
            this.Constructor
               .Add("INSERT INTO ")
               .Add(this.GetTableName(false))
               .Add(" (")
               .AddJoin(", ", columns.Select(this.Info.Config.ApplyNomenclature))
               .Add(") ");
        }

        protected void AppendInsertCells(IEnumerable<Cell> cells)
        {
            this.Constructor.Add('(');
            this.AppendCells(cells);
            this.Constructor.Add(")");
        }

        protected override void ConfigureCount(Column column)
        {
            bool safeDistinct = (column == null || column == Column.All) && this.Query.Distinct;

            if (safeDistinct)
                this.Constructor.Add("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, safeDistinct ? null : column);

            if (safeDistinct)
                this.Constructor.Add(") `count`");
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn)
        {
            bool isCount = countColumn != null;
            this.Constructor.Add("SELECT ");

            if (isCount)
                this.Constructor.Add("COUNT(");

            if (this.Query.Distinct)
                this.Constructor.Add("DISTINCT ");

            if (isCount)
            {
                WriteSelect(countColumn);
                this.Constructor.Add(')');
            }
            else
            {
                this.WriteSelectColumns();
            }

            this.Constructor.Add(" FROM ").Add(this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.WritePagination();
        }

        private void WritePagination()
        {
            if (this.Query.Limit is null && this.Query.Offset is null)
                return;

            this.Constructor.Add(" LIMIT ").Add(this.Query.Limit ?? int.MaxValue);

            if (this.Query.Offset != null)
                this.Constructor.Add(" OFFSET ").Add(this.Query.Offset);
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

                this.Constructor.Add("UPDATE ").Add(this.GetTableName(false));
                this.ApplyJoins();

                this.Constructor.Add(" SET ");
                this.Constructor.AddJoin(WriteUpdateCell, ", ", en);

                this.WriteWhere(true);
            }

            this.AddLimit();
        }

        private void AddLimit()
        {
            if (this.Query.Limit >= 0)
                this.Constructor.Add(" LIMIT ").Add(this.Query.Limit);
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

            this.Constructor
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

            this.Constructor.Add(" WHERE ");
            if (configureParameters) this.WriteWhereContent(this.Info);
            else this.Constructor.Add(this.Info.Where);
        }

        protected void WriteWhereContent(QueryInfo info)
        {
            this.Constructor.AddAndReplace(
                info.Where.ToString(),
                '?',
                (count) => this.Constructor.AddParameter(info.Where.Parameters[count - 1])
            );
        }

        protected void ThrowOffsetNotSupported()
        {
            if (this.Query.Offset is int val && val != 0)
                throw new NotSupportedException("Offset is not supported in this operation.");
        }
    }
}
