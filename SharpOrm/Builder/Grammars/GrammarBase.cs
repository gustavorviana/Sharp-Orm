using System.Collections.Generic;
using System;

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
        public QueryInfo Info => this.Query.Info;

        public GrammarBase(Query query)
        {
            this.builder = new QueryBuilder(query);
            this.Query = query;
        }

        public GrammarBase(Query query, QueryBuilder builder)
        {
            this.builder = builder;
            this.Query = query;
        }

        /// <summary>
        /// Applies the order by clause to the query.
        /// </summary>
        protected virtual void ApplyOrderBy()
        {
            this.ApplyOrderBy(this.Info.Orders, false);
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
                this.builder.Add(" ORDER BY ");

            WriteColumnOrder(en.Current);

            while (en.MoveNext())
            {
                this.builder.Add(", ");
                this.WriteColumnOrder(en.Current);
            }
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
                .Add(GetTableName(join, true))
                .Add(" ON ");

            this.WriteWhereContent(join.Info);
        }

        /// <summary>
        /// Gets the table name with or without the alias.
        /// </summary>
        /// <param name="withAlias">Whether to include the alias.</param>
        /// <returns>The table name.</returns>
        protected string GetTableName(bool withAlias)
        {
            return this.GetTableName(this.Query, withAlias);
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
            this.builder.Add(info.Where.ToExpression(true));
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (this.Info.Where.Empty && this.Info.Where.Trashed == Trashed.With)
                return;

            this.builder.Add(" WHERE ");
            if (configureParameters) this.WriteWhereContent(this.Info);
            else this.builder.Add(this.Info.Where);
        }

        /// <summary>
        /// Writes the group by clause to the query.
        /// </summary>
        protected virtual void WriteGroupBy()
        {
            if (this.Info.GroupsBy.Length == 0)
                return;

            this.builder.Add(" GROUP BY ");
            AddParams(this.Info.GroupsBy, null, false);
            if (this.Info.Having.Empty)
                return;

            this.builder
                .Add(" HAVING ")
                .AddAndReplace(
                    Info.Having.ToString(),
                    '?',
                    (count) => this.builder.AddParameter(Info.Having.Parameters[count - 1])
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

                this.builder.AddParameter(call(en.Current), allowAlias);

                while (en.MoveNext())
                    this.builder.Add(", ").AddParameter(call(en.Current), allowAlias);
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

            this.WriteColumn(order.Column, false);
            this.builder.Add(' ');
            this.builder.Add(order.Order.ToString().ToUpper());
        }

        /// <summary>
        /// Writes the column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteColumn(Column column, bool allowAlias = true)
        {
            this.builder.Add(column.ToSafeExpression(this.Info.ToReadOnly(), allowAlias));
        }
    }
}
