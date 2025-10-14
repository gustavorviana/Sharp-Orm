using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Builder class for constructing typed WHERE clauses.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class WhereBuilder<T> : QueryBase, ITypedWhereClause<T, WhereBuilder<T>>, ITypedQuery<T>
    {
        private readonly TableInfo _tableInfo;

        /// <summary>
        /// Gets the table information for the entity type.
        /// </summary>
        public TableInfo TableInfo => _tableInfo;

        /// <summary>
        /// Creates a new instance of WhereBuilder with specific configuration.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        internal WhereBuilder(QueryConfig config, DbName tableName) : base(config, tableName)
        {
            _tableInfo = config.Translation.GetTable(typeof(T));
        }

        private static string GetTableName(QueryConfig config)
        {
            var registry = config?.Translation ?? TranslationRegistry.Default;
            return registry.GetTableName(typeof(T));
        }

        /// <summary>
        /// Adds a WHERE clause using a column expression, operation, and value.
        /// </summary>
        public WhereBuilder<T> Where<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression, string operation, TValue value)
        {
            var column = GetColumn(columnExpression);
            base.Where(column, operation, value);
            return this;
        }

        /// <summary>
        /// Adds an OR WHERE clause using a column expression, operation, and value.
        /// </summary>
        public WhereBuilder<T> OrWhere<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression, string operation, TValue value)
        {
            var column = GetColumn(columnExpression);
            base.OrWhere(column, operation, value);
            return this;
        }


        public WhereBuilder<T> Where(WhereBuilder<T> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (builder == this) throw new InvalidOperationException();

            base.Info.Where.Add(builder.Info.Where);

            return this;
        }

        WhereBuilder<T> IWhereClause<WhereBuilder<T>>.Where(object column, string operation, object value)
        {
            base.Where(column, operation, value);
            return this;
        }

        WhereBuilder<T> IWhereClause<WhereBuilder<T>>.Where(SqlExpression expression)
        {
            base.Where(expression);
            return this;
        }

        WhereBuilder<T> IWhereClause<WhereBuilder<T>>.OrWhere(object column, string operation, object value)
        {
            base.OrWhere(column, operation, value);
            return this;
        }

        WhereBuilder<T> IWhereClause<WhereBuilder<T>>.OrWhere(SqlExpression expression)
        {
            base.OrWhere(expression);
            return this;
        }

        public WhereBuilder<T> Where(QueryCallback<T> callback)
        {
            return WriteCallback(callback, AND);
        }

        public WhereBuilder<T> OrWhere(QueryCallback<T> callback)
        {
            return WriteCallback(callback, OR);
        }

        private WhereBuilder<T> WriteCallback(QueryCallback<T> callback, string whereType)
        {
            var qBase = new WhereBuilder<T>(Info.Config, Info.TableName);
            callback(qBase);

            WrapWithParentheses(qBase, whereType);
            return this;
        }

        #region ITypedQuery<T> Implementation

        /// <summary>
        /// Converts a column expression to a Column object.
        /// </summary>
        public Column GetColumn<TValue>(Expression<ColumnExpression<T, TValue>> columnExpression)
        {
            var processor = new ExpressionProcessor<T>(Info, Info.Config.Translation, ExpressionConfig.All, null);
            return processor.ParseColumn(columnExpression);
        }

        ExpressionColumn[] ITypedQuery<T>.GetColumns(Expression<ColumnExpression<T>> expression)
        {
            return GetColumns(expression);
        }

        /// <summary>
        /// Converts a column expression to an array of ExpressionColumn objects.
        /// </summary>
        internal ExpressionColumn[] GetColumns(Expression<ColumnExpression<T>> expression)
        {
            return GetColumns(expression, ExpressionConfig.All);
        }

        /// <summary>
        /// Converts a column expression to an array of ExpressionColumn objects with specific configuration.
        /// </summary>
        internal ExpressionColumn[] GetColumns(Expression<ColumnExpression<T>> expression, ExpressionConfig config)
        {
            var processor = new ExpressionProcessor<T>(Info, Info.Config.Translation, config, null);
            return processor.ParseColumns(expression).ToArray();
        }

        ExpressionColumn[] ITypedQuery<T>.GetColumns(Expression<ColumnExpression<T>> expression, ExpressionConfig config)
        {
            return GetColumns(expression, config);
        }

        #endregion
    }
}
