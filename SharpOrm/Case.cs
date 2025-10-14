using SharpOrm.Builder;
using SharpOrm.Msg;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpOrm
{
    /// <summary>
    /// Represents a SQL CASE statement for conditional expressions.
    /// </summary>
    public class Case : Column
    {
        private readonly List<CaseNode> _nodes = new List<CaseNode>();
        private object _elseValue;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Case"/> class.
        /// </summary>
        public Case()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Case"/> class by copying the specified column.
        /// </summary>
        /// <param name="column">The column to copy.</param>
        public Case(Column column) : base(column)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Case"/> class with the specified database column name.
        /// </summary>
        /// <param name="column">The database column name.</param>
        public Case(DbName column) : base(column)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Case"/> class with the specified column name.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        public Case(string columnName) : base(columnName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Case"/> class with the specified column name and alias.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="alias">The alias for the column.</param>
        public Case(string columnName, string alias) : base(columnName, alias)
        {
        }

        #endregion

        #region When

        /// <summary>
        /// Adds a condition to the CASE statement that checks if the specified column is null.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        public Case WhenNull(string column, object then)
        {
            _nodes.Add(new ExpressionCaseNode(new Column(column), new SqlExpression("IS NULL"), then));
            return this;
        }

        /// <summary>
        /// Adds a condition to the CASE statement that checks if the specified column is not null.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        public Case WhenNotNull(string column, object then)
        {
            _nodes.Add(new ExpressionCaseNode(new Column(column), new SqlExpression("IS NOT NULL"), then));
            return this;
        }

        /// <summary>
        /// Adds a condition to the CASE statement with the specified column, operation, and value.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <param name="value">The value to compare with.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        public Case When(string column, object value, object then)
        {
            return When(column, "=", value, then);
        }

        /// <summary>
        /// Adds a condition to the CASE statement with the specified SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression representing the condition.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value is a collection.</exception>
        public Case When(SqlExpression expression, object then)
        {
            if (then is ICollection)
                throw new NotSupportedException();

            _nodes.Add(new ExpressionCaseNode(expression, then));
            return this;
        }

        /// <summary>
        /// Adds a condition to the CASE statement with the specified column, operation, value, and return value.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <param name="operation">The operation to perform.</param>
        /// <param name="value">The value to compare with.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value is a collection.</exception>
        public Case When(string column, string operation, object value, object then)
        {
            QueryBase.CheckIsAvailableOperation(operation);
            if (then is ICollection)
                throw new NotSupportedException();

            _nodes.Add(new ExpressionCaseNode(new Column(column), new SqlExpression(string.Concat(operation, " ?"), value), then));
            return this;
        }

        /// <summary>
        /// Adds a condition to the CASE statement with the specified expected value and return value.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="then">The value to return if the condition is met.</param>
        /// <returns>The updated <see cref="Case"/> instance.</returns>
        public Case When(object expected, object then)
        {
            _nodes.Add(new ExpressionCaseNode(expected is SqlExpression exp ? exp : new SqlExpression("?", expected), then));
            return this;
        }

        public Case When(QueryCallback callback, object then)
        {
            _nodes.Add(new QueryCaseNode(callback, then));
            return this;
        }

        #endregion

        /// <summary>
        /// Specifies the value to return if none of the conditions are met.
        /// </summary>
        /// <param name="value">The value to return if no conditions are met.</param>
        /// <returns>The updated <see cref="Column"/> instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value is a collection.</exception>
        public Column Else(object value)
        {
            if (value is ICollection)
                throw new NotSupportedException();

            _elseValue = value == null ? DBNull.Value : value;
            return this;
        }

        /// <summary>
        /// Converts the CASE statement to a SQL expression.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <param name="alias">A value indicating whether to include an alias.</param>
        /// <returns>The SQL expression representing the CASE statement.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the CASE statement has no conditions.</exception>
        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            if (_nodes.Count == 0)
                throw new InvalidOperationException(Messages.CannotUseEmptyCase);

            var query = new QueryBuilder(info);
            this.WriteCase(query, info);

            foreach (var node in _nodes)
                node.ApplyTo(query, info);

            if (_elseValue != null)
                query.Add("ELSE ").AddParameter(_elseValue).Add();

            query.Add("END");

            if (alias && !string.IsNullOrEmpty(this.Alias))
                query.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return query.ToExpression(info);
        }

        private QueryBuilder WriteCase(QueryBuilder query, IReadonlyQueryInfo info)
        {
            query.Add("CASE");

            if (_expression != null)
                return query.Add().Add(_expression, false).Add();

            if (!string.IsNullOrEmpty(this.Name))
                return query.Add(" ").Add(info.Config.ApplyNomenclature(this.Name)).Add(" ");

            return query.Add();
        }

        private abstract class CaseNode
        {
            private readonly Column _column;
            private readonly object _then;

            public CaseNode(Column column, object then)
            {
                _column = column;
                _then = then;
            }

            public QueryBuilder ApplyTo(QueryBuilder query, IReadonlyQueryInfo info)
            {
                query.Add("WHEN ");

                if (_column != null)
                    query.AddParameter(_column).Add();

                WriteTo(query, info);

                query.Add(" THEN ");

                if (_then is SqlExpression exp)
                    return query.Add().AddParameter(exp).Add();

                return query.AddParameter(_then).Add();
            }

            protected abstract void WriteTo(QueryBuilder query, IReadonlyQueryInfo info);
        }

        private class ExpressionCaseNode : CaseNode
        {
            private readonly SqlExpression _expression;

            public ExpressionCaseNode(Column column, SqlExpression expression, object then) : base(column, then)
            {
                _expression = expression;
            }

            public ExpressionCaseNode(SqlExpression expression, object then) : base(null, then)
            {
                _expression = expression;
            }

            protected override void WriteTo(QueryBuilder query, IReadonlyQueryInfo info)
                => query.Add(_expression, false);
        }

        private class QueryCaseNode : CaseNode
        {
            private readonly QueryCallback _callback;

            public QueryCaseNode(QueryCallback callback, object then) : base(null, then)
            {
                _callback = callback;
            }

            protected override void WriteTo(QueryBuilder query, IReadonlyQueryInfo info)
            {
                var qBase = new QueryBase(info.Config, info.TableName);
                _callback(qBase);
                qBase.Info.Where.ApplyTo(query);
            }
        }
    }
}
