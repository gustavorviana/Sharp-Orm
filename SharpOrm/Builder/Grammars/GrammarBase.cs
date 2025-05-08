using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
{
    public abstract class GrammarBase : IGrammarBase
    {
        QueryBuilder IGrammarBase.Builder => Builder;

        /// <summary>
        /// Gets the query Builder.
        /// </summary>
        protected QueryBuilder Builder { get; private set; }

        /// <summary>
        /// Gets the query.
        /// </summary>
        protected Query Query { get; }

        /// <summary>
        /// Gets the query information.
        /// </summary>
        public QueryInfo Info => Query.Info;

        /// <summary>
        /// Gets the translation registry for the current query configuration.
        /// </summary>
        protected TranslationRegistry Translation => Query.Info.Config.Translation;

        public GrammarBase(Query query)
        {
            Builder = new QueryBuilder(query);
            Query = query;
        }

        public GrammarBase(Query query, QueryBuilder builder)
        {
            this.Builder = builder;
            Query = query;
        }

        public GrammarBase(GrammarBase owner)
        {
            Builder = owner.Builder;
            Query = owner.Query;
        }

        public GrammarBase(Query query, bool useLotQueryBuilder)
        {
            Builder = useLotQueryBuilder ? new BatchQueryBuilder(query.Info) : new QueryBuilder(query.Info);
            Query = query;
        }

        protected bool CanWriteOrderby()
        {
            return Info.Select.Length != 1 || !Info.Select[0].IsCount;
        }

        protected void SetParamInterceptor(Func<object, object> func)
        {
            Builder.paramInterceptor = func;
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
                Builder.Add(" ORDER BY ");

            WriteColumnOrder(en.Current);

            while (en.MoveNext())
            {
                Builder.Add(", ");
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

            Builder
                .Add(' ')
                .Add(join.Type)
                .Add(" JOIN ");
            WriteTable(join);
            Builder.Add(" ON ");

            WriteWhereContent(join.Info);
        }

        /// <summary>
        /// Writes the update cell to the query.
        /// </summary>
        /// <param name="cell">The cell.</param>
        protected void WriteUpdateCell(Cell cell)
        {
            Builder.Add(FixColumnName(cell.Name)).Add(" = ");
            Builder.AddParameter(cell.Value);
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
            Builder.Add(GetTableName(query, true));
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
            Builder.Add(info.Where.ToExpression(true));
        }

        protected void WriteWhere(bool configureParameters)
        {
            if (Info.Where.Empty && Info.Where.Trashed == Trashed.With)
                return;

            Builder.Add(" WHERE ");
            if (configureParameters) WriteWhereContent(Info);
            else Builder.Add(Info.Where);
        }

        /// <summary>
        /// Writes the group by clause to the query.
        /// </summary>
        protected virtual void WriteGroupBy()
        {
            if (Info.GroupsBy.Length == 0)
                return;

            Builder.Add(" GROUP BY ");
            AddParams(Info.GroupsBy, null, false);
            if (Info.Having.Empty)
                return;

            var havingParams = Info.Having.ToExpression(true, false);

            Builder
                .Add(" HAVING ")
                .AddAndReplace(
                    havingParams.ToString(),
                    '?',
                    (count) => Builder.AddParameter(havingParams.Parameters[count - 1])
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

                Builder.AddParameter(call(en.Current), allowAlias);

                while (en.MoveNext())
                    Builder.Add(", ").AddParameter(call(en.Current), allowAlias);
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
            Builder.Add(' ');
            Builder.Add(order.Order.ToString().ToUpper());
        }

        /// <summary>
        /// Writes the column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteColumn(Column column, bool allowAlias = true)
        {
            Builder.Add(column.ToSafeExpression(Info.ToReadOnly(), allowAlias));
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

        protected string TryGetAlias(QueryBase query)
        {
            return query.Info.TableName.TryGetAlias(Info.Config);
        }

        protected string TryGetAlias(DbName name)
        {
            return name.TryGetAlias(Info.Config);
        }

        /// <summary>
        /// Writes the select columns to the query.
        /// </summary>
        protected virtual void WriteSelectColumns()
        {
            AddParams(this.Info.Select);
        }

        protected IEnumerable<ColumnInfo> GetPrimaryKeys()
        {
            var tableinfo = Query.GetTableInfo();
            return tableinfo == null ? DotnetUtils.EmptyArray<ColumnInfo>() : tableinfo.Columns.Where(c => c.Key);
        }

        /// <summary>
        /// Writes the select column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteSelect(Column column)
        {
            this.Builder.AddExpression(column, true);
        }

        protected void ThrowOffsetNotSupported()
        {
            if (Query.Offset.HasValue && Query.Offset.Value > 0)
                throw new NotSupportedException();
        }

        protected void ThrowLimitNotSupported()
        {
            if (Query.Limit.HasValue && Query.Limit.Value > 0)
                throw new NotSupportedException(Messages.Grammar.OffsetNotSupported);
        }

        protected void ThrowJoinNotSupported()
        {
            if (Query.Info.Joins.Count > 0)
                throw new NotSupportedException(Messages.Grammar.JoinNotSupported);
        }

        protected void ThrowOrderNotSupported()
        {
            if (Query.Info.Orders.Length > 0)
                throw new NotSupportedException(Messages.Grammar.OrderByNotSupported);
        }
    }
}
