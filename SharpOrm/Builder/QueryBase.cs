﻿using SharpOrm.Errors;
using System;
using System.Collections;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// SQL clause creator. Provides methods to build SQL SELECT, INSERT, UPDATE and DELETE queries. Allows defining WHERE, ORDER BY, GROUP BY, JOIN and other clauses.
    /// </summary>
    public class QueryBase : IDisposable
    {
        #region Fields\Const
        internal const string AND = "AND";
        internal const string OR = "OR";

        private bool _ignoreWhereType = false;
        private bool _disposed = false;
        protected internal QueryInfo Info { get; }

        public bool Disposed => this._disposed;
        private static string[] AvailableOperations { get; } = {
            "=",
            ">",
            "<",
            ">=",
            "<=",
            "<>",
            "!=",
            "!>",
            "!<",
            "like",
            "in",
            "not in",
            "not like",
            "is",
            "is not"
        };
        #endregion

        /// <summary>
        /// Initializes a new instance of the QueryBase class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use for the query.</param>
        public QueryBase(IQueryConfig config, DbName table)
        {
            this.Info = new QueryInfo(config, table);
        }

        #region Where

        /// <summary>
        /// Adds a clause to the "WHERE" statement based on an ISqlExpressible object, where the expression is safely converted to a SqlExpression.
        /// </summary>
        /// <param name="expressible">The ISqlExpressible object that contains the expression to be added to the WHERE statement.</param>
        /// <returns>The QueryBase instance to allow for method chaining.</returns>
        public QueryBase Where(ISqlExpressible expressible, bool allowAlias = false)
        {
            return this.Where(expressible.ToSafeExpression(this.Info.ToReadOnly(), allowAlias));
        }

        /// <summary>
        /// Adds the sql clause to the "WHERE" (If there are any previous clauses, "AND" is inserted before the new clause)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase Where(SqlExpression expression)
        {
            this.WriteWhereType(AND);
            this.Info.Where.AddParameter(expression);
            return this;
        }

        /// <summary>
        /// Add a clusule (column=value) to the "WHERE" (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase Where(string column, object value)
        {
            return this.Where(column, value is null ? "IS" : "=", value);
        }

        /// <summary>
        /// Add a clusule (column!=value) to the "WHERE" (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase WhereNot(string column, object value)
        {
            return this.Where(column, value is null ? "IS NOT" : "!=", value);
        }

        /// <summary>
        /// This method adds a clause to the "WHERE" clause checking if a column is null
        /// </summary>
        /// <param name="column">the name of the column to be checked</param>
        public QueryBase WhereNull(string column)
        {
            return this.Where(column, "IS", null);
        }

        /// <summary>
        /// This method adds a clause to the "WHERE" clause checking if a column is not null
        /// </summary>
        /// <param name="column">the name of the column to be checked</param>
        public QueryBase WhereNotNull(string column)
        {
            return this.Where(column, "IS NOT", null);
        }

        /// <summary>
        /// Adds a clause to "WHERE" (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to check.</param>
        /// <param name="operation">Clause operator.</param>
        /// <param name="value">Value to check.</param>
        /// <returns></returns>
        public QueryBase Where(string column, string operation, object value)
        {
            return this.WriteWhere(column, operation, value, AND);
        }

        /// <summary>
        /// Add a clause in parentheses (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="callback">Callback where the clause should be builded.</param>
        /// <returns></returns>
        public QueryBase Where(QueryCallback callback)
        {
            return this.WriteCallback(callback, AND);
        }

        /// <summary>
        /// Adds a value comparison clause between columns in "WHERE"
        /// </summary>
        /// <param name="column1">Column 1 to compare value.</param>
        /// <param name="operation">Operation</param>
        /// <param name="column2">Column 2 to compare value.</param>
        /// <returns></returns>
        public QueryBase WhereColumn(string column1, string operation, string column2)
        {
            CheckIsAvailableOperation(operation);
            this.WriteWhereType(AND);

            this.Info.Where.Add(this.Info.Config.ApplyNomenclature(column1), operation, this.Info.Config.ApplyNomenclature(column2));
            return this;
        }

        /// <summary>
        /// Check if the value is between two others.
        /// </summary>
        /// <param name="toCheck">Value to check.</param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public QueryBase WhereBetween(object toCheck, object arg1, object arg2)
        {
            return this.WriteBetween(toCheck, arg1, arg2, false, AND);
        }

        /// <summary>
        /// Check if the value is not between two others.
        /// </summary>
        /// <param name="toCheck">Value to check.</param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public QueryBase WhereNotBetween(object toCheck, object arg1, object arg2)
        {
            return this.WriteBetween(toCheck, arg1, arg2, true, AND);
        }

        /// <summary>
        /// Adds an EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="query">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase Exists(Query query)
        {
            return this.WriteExists(query, false, AND);
        }

        /// <summary>
        /// Adds an EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="exp">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase Exists(SqlExpression exp)
        {
            return this.WriteExists(exp, false, AND);
        }

        /// <summary>
        /// Adds an NOT EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="query">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase NotExists(Query query)
        {
            return this.WriteExists(query, true, AND);
        }

        /// <summary>
        /// Adds an NOT EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="exp">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase NotExists(SqlExpression exp)
        {
            return this.WriteExists(exp, true, AND);
        }

        #endregion

        #region OrWhere

        /// <summary>
        /// Adds an OR condition to the WHERE clause of the query.
        /// </summary>
        public QueryBase OrWhere(ISqlExpressible expressible, bool allowAlias = false)
        {
            return this.OrWhere(expressible.ToSafeExpression(this.Info.ToReadOnly(), allowAlias));
        }

        /// <summary>
        ///  Adds an OR condition to the WHERE clause of the query.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase OrWhere(SqlExpression expression)
        {
            this.WriteWhereType(OR);
            this.Info.Where.AddParameter(expression);
            return this;
        }

        /// <summary>
        /// Add a clusule (column=value) to the "WHERE" (If there are any previous clauses, "OR" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase OrWhere(string column, object value)
        {
            return this.OrWhere(column, value is null ? "IS" : "=", value);
        }

        /// <summary>
        /// Add a clusule (column!=value) to the "WHERE" (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase OrWhereNot(string column, object value)
        {
            return this.OrWhere(column, value is null ? "IS NOT" : "!=", value);
        }

        /// <summary>
        /// Adds an OR condition to the "WHERE" clause checking if a column is null
        /// </summary>
        /// <param name="column">the name of the column to be checked</param>
        public QueryBase OrWhereNull(string column)
        {
            return this.OrWhere(column, "IS", null);
        }

        /// <summary>
        /// Adds an OR condition to the "WHERE" clause checking if a column is not null
        /// </summary>
        /// <param name="column">the name of the column to be checked</param>
        public QueryBase OrWhereNotNull(string column)
        {
            return this.OrWhere(column, "IS NOT", null);
        }

        /// <summary>
        /// Adds a clause to "WHERE" (If there are any previous clauses, "OR" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to check.</param>
        /// <param name="operation">Clause operator.</param>
        /// <param name="value">Value to check.</param>
        /// <returns></returns>
        public QueryBase OrWhere(string column, string operation, object value)
        {
            return this.WriteWhere(column, operation, value, OR);
        }

        /// <summary>
        /// Add a clause in parentheses (If there are any previous clauses, "OR" is entered before the new clause).
        /// </summary>
        /// <param name="callback">Callback where the clause should be builded.</param>
        /// <returns></returns>
        public QueryBase OrWhere(QueryCallback callback)
        {
            return this.WriteCallback(callback, OR);
        }

        /// <summary>
        /// Adds a value comparison clause between columns in "WHERE"
        /// </summary>
        /// <param name="column1">Column 1 to compare value.</param>
        /// <param name="operation">Operation</param>
        /// <param name="column2">Column 2 to compare value.</param>
        /// <returns></returns>
        public QueryBase OrWhereColumn(string column1, string operation, string column2)
        {
            CheckIsAvailableOperation(operation);
            this.WriteWhereType(OR);

            this.Info.Where.Add(this.Info.Config.ApplyNomenclature(column1), operation, this.Info.Config.ApplyNomenclature(column2));
            return this;
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a value is between two others.
        /// </summary>
        /// <param name="toCheck">The value to check.</param>
        /// <param name="arg1">The first comparison value.</param>
        /// <param name="arg2">The second comparison value.</param>
        public QueryBase OrWhereBetween(object toCheck, object arg1, object arg2)
        {
            return this.WriteBetween(toCheck, arg1, arg2, false, OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a value is not between two others.
        /// </summary>
        /// <param name="toCheck">The value to check.</param>
        /// <param name="arg1">The first comparison value.</param>
        /// <param name="arg2">The second comparison value.</param>
        public QueryBase OrWhereNotBetween(object toCheck, object arg1, object arg2)
        {
            return this.WriteBetween(toCheck, arg1, arg2, true, OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a subquery returns any rows.
        /// </summary>
        /// <param name="query">The subquery to check.</param>
        public QueryBase OrExists(Query query)
        {
            return this.WriteExists(query, false, OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a subquery returns any rows.
        /// </summary>
        /// <param name="exp">The subquery to check.</param>
        public QueryBase OrExists(SqlExpression exp)
        {
            return this.WriteExists(exp, false, OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a subquery returns no rows.
        /// </summary>
        /// <param name="query">The subquery to check.</param>
        public QueryBase OrNotExists(Query query)
        {
            return this.WriteExists(query, true, OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a subquery returns no rows.
        /// </summary>
        /// <param name="exp">The subquery to check.</param>
        public QueryBase OrNotExists(SqlExpression exp)
        {
            return this.WriteExists(exp, true, OR);
        }

        #endregion

        #region Where builder

        private QueryBase WriteCallback(QueryCallback callback, string whereType)
        {
            try
            {
                this.WriteWhereType(whereType);
                this._ignoreWhereType = true;

                this.Info.Where.Add('(');
                callback(this);
            }
            finally
            {
                this.Info.Where.FixLastOpen();
            }

            return this;
        }

        private QueryBase WriteExists(object queryObj, bool not, string whereType)
        {
            this.WriteWhereType(whereType);

            if (not) this.Info.Where.Add("NOT ");
            this.Info.Where.Add("EXISTS ");

            if (queryObj is Query query) this.WriteQuery(query);
            else this.Info.Where.Add('(').AddParameter(queryObj).Add(')');

            return this;
        }

        private QueryBase WriteBetween(object toCheck, object arg1, object arg2, bool isNot, string whereType)
        {
            this.WriteWhereType(whereType);
            this.WriteBetweenArgument(toCheck);

            if (isNot)
                this.Info.Where.Add(" NOT");

            this.Info.Where.Add(" BETWEEN ");
            this.WriteBetweenArgument(arg1);
            this.Info.Where.Add(" AND ");
            this.WriteBetweenArgument(arg2);

            return this;
        }

        private QueryConstructor WriteBetweenArgument(object arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            if (arg is string strColumn)
                return this.Info.Where.Add(this.Info.Config.ApplyNomenclature(strColumn));

            if (arg is SqlExpression || arg is ISqlExpressible || arg is DateTime || arg is TimeSpan || arg.GetType().IsPrimitive || arg is Enum)
                return this.Info.Where.AddParameter(arg);

            throw new InvalidOperationException("The column type is invalid. Use an Expression or string type.");
        }

        internal protected QueryBase WriteWhere(object column, string operation, object value, string type)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            CheckIsAvailableOperation(operation);
            bool isExpressionList = (value is SqlExpression || value is ISqlExpressible) && (operation == "IN" || operation == "NOT IN");

            this.WriteWhereType(type);
            this.ParseColumn(column);
            this.Info.Where.Add().Add(operation).Add();

            if (isExpressionList)
                this.Info.Where.Add('(');

            this.WriteValue(value);

            if (isExpressionList)
                this.Info.Where.Add(')');

            return this;
        }

        /// <summary>
        /// Checks whether the inserted operation has been recorded in "this.AvailableOperations".
        /// </summary>
        /// <param name="operation"></param>
        internal protected static void CheckIsAvailableOperation(string operation)
        {
            if (!AvailableOperations.Contains(operation.ToLower()))
                throw new DatabaseException("Invalid SQL operation: " + operation);
        }

        /// <summary>
        /// Turns the column object into a sql value.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        protected QueryConstructor ParseColumn(object column)
        {
            if (column is string strColumn)
                return this.Info.Where.Add(this.Info.Config.ApplyNomenclature(strColumn));

            if (column is ISqlExpressible iExp)
                column = iExp.ToSafeExpression(this.Info.ToReadOnly(), true);

            if (column is SqlExpression exp)
                return this.Info.Where.Add(exp);

            throw new NotSupportedException("The column type is not supported.");
        }

        /// <summary>
        /// Loads the value object and converts it to a sql expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected QueryConstructor WriteValue(object value)
        {
            if (value is ICollection collection)
                return this.Info.Where.WriteEnumerableAsValue(collection, true);

            if (value is Query query)
                return this.WriteQuery(query);

            return this.Info.Where.AddParameter(value);
        }

        private QueryConstructor WriteQuery(Query query)
        {
            return this.Info.Where.Add('(').Add(query.ToString()).Add(')').AddParameters(query.Info.Where.Parameters);
        }

        internal void WriteWhereType(string type)
        {
            if (!this.Info.Where.Empty && !_ignoreWhereType)
                this.Info.Where.Add("", type, "");

            if (_ignoreWhereType)
                _ignoreWhereType = false;
        }

        #endregion

        #region IDisposed

        ~QueryBase()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            this._disposed = true;
        }

        public void Dispose()
        {
            if (this._disposed)
                throw new ObjectDisposedException(this.GetType().Name);

            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
