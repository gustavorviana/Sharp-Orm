using System.Collections.Generic;
using System;
using SharpOrm.Msg;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public abstract class GrammarBase
    {
        /// <summary>
        /// Gets the query builder.
        /// </summary>
        protected QueryBuilder builder { get; }

        /// <summary>
        /// Gets the query.
        /// </summary>
        protected Query Query { get; }

        /// <summary>
        /// Gets the query information.
        /// </summary>
        public QueryInfo Info => Query.Info;

        public GrammarBase(Query query)
        {
            builder = new QueryBuilder(query);
            Query = query;
        }

        public GrammarBase(Query query, QueryBuilder builder)
        {
            this.builder = builder;
            Query = query;
        }

        public GrammarBase(GrammarBase owner)
        {
            builder = owner.builder;
            Query = owner.Query;
        }

        protected bool CanWriteOrderby()
        {
            return Info.Select.Length != 1 || !Info.Select[0].IsCount;
        }

        /// <summary>
        /// Applies the order by clause to the query.
        /// </summary>
        protected virtual void ApplyOrderBy()
        {
            ApplyOrderBy(Info.Orders, false);
        }

        /// <summary>
        /// Applies the order by clause to the query.
        /// </summary>
        /// <param name="order">The order by columns.</param>
        /// <param name="writeOrderByFlag">Indicates whether to write the ORDER BY keyword.</param>
        protected virtual void ApplyOrderBy(IEnumerable<ColumnOrder> order, bool writeOrderByFlag)
        {
            var en = order.GetEnumerator();
            if (!en.MoveNext())
                return;

            if (!writeOrderByFlag)
                builder.Add(" ORDER BY ");

            WriteColumnOrder(en.Current);

            while (en.MoveNext())
            {
                builder.Add(", ");
                WriteColumnOrder(en.Current);
            }
        }

        protected void ApplyJoins()
        {
            if (Info.Joins.Count > 0)
                foreach (var join in Info.Joins)
                    WriteJoin(join);
        }

        protected virtual void WriteJoin(JoinQuery join)
        {
            if (string.IsNullOrEmpty(join.Type))
                join.Type = "INNER";

            builder
                .Add(' ')
                .Add(join.Type)
                .Add(" JOIN ");
            WriteTable(join);
            builder.Add(" ON ");

            WriteWhereContent(join.Info);
        }

        /// <summary>
        /// Writes the update cell to the query.
        /// </summary>
        /// <param name="cell">The cell.</param>
        protected void WriteUpdateCell(Cell cell)
        {
            builder.Add(FixColumnName(cell.Name)).Add(" = ");
            builder.AddParameter(cell.Value);
        }

        /// <summary>
        /// Applies the nomenclature to the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name with the applied nomenclature.</returns>
        protected string FixTableName(string name)
        {
            return Info.Config.ApplyNomenclature(name);
        }

        /// <summary>
        /// Apply column prefix and suffix.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected string FixColumnName(string name)
        {
            return Info.Config.ApplyNomenclature(name);
        }

        /// <summary>
        /// Gets the table name with or without the alias.
        /// </summary>
        /// <param name="withAlias">Whether to include the alias.</param>
        /// <returns>The table name.</returns>
        protected string GetTableName(bool withAlias)
        {
            return GetTableName(Query, withAlias);
        }

        protected virtual void WriteTable(QueryBase query)
        {
            builder.Add(GetTableName(query, true));
        }

        /// <summary>
        /// Gets the table name with or without the alias.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="withAlias">Whether to include the alias.</param>
        /// <returns>The table name.</returns>
        protected string GetTableName(QueryBase query, bool withAlias)
        {
            return query.Info.TableName.GetName(withAlias, query.Info.Config);
        }

        protected void WriteWhereContent(QueryBaseInfo info)
        {
            builder.Add(info.Where.ToExpression(true));
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (Info.Where.Empty && Info.Where.Trashed == Trashed.With)
                return;

            builder.Add(" WHERE ");
            if (configureParameters) WriteWhereContent(Info);
            else builder.Add(Info.Where);
        }

        /// <summary>
        /// Writes the group by clause to the query.
        /// </summary>
        protected virtual void WriteGroupBy()
        {
            if (Info.GroupsBy.Length == 0)
                return;

            builder.Add(" GROUP BY ");
            AddParams(Info.GroupsBy, null, false);
            if (Info.Having.Empty)
                return;

            builder
                .Add(" HAVING ")
                .AddAndReplace(
                    Info.Having.ToString(),
                    '?',
                    (count) => builder.AddParameter(Info.Having.Parameters[count - 1])
                );
        }

        /// <summary>
        /// Adds the parameters to the query.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="values">The values.</param>
        /// <param name="call">The function to get the value.</param>
        protected void AddParams<T>(IEnumerable<T> values, Func<T, object> call = null, bool allowAlias = true)
        {
            if (call == null)
                call = obj => obj;

            using (var en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return;

                builder.AddParameter(call(en.Current), allowAlias);

                while (en.MoveNext())
                    builder.Add(", ").AddParameter(call(en.Current), allowAlias);
            }
        }

        /// <summary>
        /// Writes the order by column.
        /// </summary>
        /// <param name="order">The order by column.</param>
        protected void WriteColumnOrder(ColumnOrder order)
        {
            if (order.Order == OrderBy.None)
                return;

            WriteColumn(order.Column, false);
            builder.Add(' ');
            builder.Add(order.Order.ToString().ToUpper());
        }

        /// <summary>
        /// Writes the column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteColumn(Column column, bool allowAlias = true)
        {
            builder.Add(column.ToSafeExpression(Info.ToReadOnly(), allowAlias));
        }

        /// <summary>
        /// Determines whether the join can be deleted.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <returns>True if the join can be deleted; otherwise, false.</returns>
        protected bool CanDeleteJoin(QueryBaseInfo info)
        {
            string name = info.TableName.TryGetAlias(this.Info.Config);
            foreach (var jName in this.Query.deleteJoins)
                if (jName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>
        /// Tries to get the table alias for the query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The table alias.</returns>
        protected string TryGetTableAlias(QueryBase query)
        {
            return query.Info.TableName.TryGetAlias(query.Info.Config);
        }

        /// <summary>
        /// Writes the select columns to the query.
        /// </summary>
        protected virtual void WriteSelectColumns()
        {
            AddParams(this.Info.Select);
        }

        /// <summary>
        /// Writes the select column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteSelect(Column column)
        {
            this.builder.AddExpression(column, true);
        }

        protected bool IsMultipleTablesDeleteWithJoin()
        {
            return Query.deleteJoins?.Any() ?? false;
        }

        protected void ThrowOffsetNotSupported()
        {
            if (Query.Offset.HasValue && Query.Offset.Value > 0)
                throw new NotSupportedException();
        }

        protected void ThrowLimitNotSupported()
        {
            if (Query.Limit.HasValue && Query.Limit.Value > 0)
                throw new NotSupportedException(Messages.GrammarMessage.OffsetNotSupported);
        }

        protected void ThrowJoinNotSupported()
        {
            if (Query.Info.Joins.Count > 0)
                throw new NotSupportedException(Messages.GrammarMessage.JoinNotSupported);
        }

        protected void ThrowOrderNotSupported()
        {
            if (Query.Info.Orders.Length > 0)
                throw new NotSupportedException(Messages.GrammarMessage.OrderByNotSupported);
        }
    }
}
