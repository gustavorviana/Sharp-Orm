using SharpOrm.Errors;
using System;
using System.Linq;
using System.Text;

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

        private bool _disposed = false;
        protected internal QueryInfo Info { get; }
        #endregion

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

        /// <summary>
        /// Initializes a new instance of the QueryBase class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use for the query.</param>
        public QueryBase(IQueryConfig config)
        {
            this.Info = new QueryInfo(config);
        }

        #region Where

        /// <summary>
        /// Adds a clause to the "WHERE" statement based on an ISqlExpressible object, where the expression is safely converted to a SqlExpression.
        /// </summary>
        /// <param name="expressible">The ISqlExpressible object that contains the expression to be added to the WHERE statement.</param>
        /// <returns>The QueryBase instance to allow for method chaining.</returns>
        public QueryBase Where(ISqlExpressible expressible)
        {
            return this.Where(expressible.ToSafeExpression(this.Info.ToReadOnly(), false));
        }

        /// <summary>
        /// Adds the sql clause to the "WHERE" (If there are any previous clauses, "AND" is inserted before the new clause)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase Where(SqlExpression expression)
        {
            return this.WriteWhere(ToSql(expression), AND);
        }

        /// <summary>
        /// Add a clusule (column=value) to the "WHERE" (If there are any previous clauses, "AND" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase Where(string column, object value)
        {
            return this.Where(column, value == null ? "IS" : "=", value);
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
            var query = new QueryBase(this.Info.Config);
            callback(query);

            if (query.Info.Where.Parameters.Count > 0)
            {
                this.Info.Where.AddParams(query.Info.Where.Parameters);
                return this.WriteWhere($"({query.Info.Where})", AND);
            }

            return this;
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

            column1 = this.Info.Config.ApplyNomenclature(column1);
            column2 = this.Info.Config.ApplyNomenclature(column2);

            return this.WriteWhere($"{column1} {operation} {column2}", AND);
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

        /// Adds an EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="query">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase Exists(Query query)
        {
            return this.WriteWhere($"EXISTS {this.RegisterQuery(query)}", AND);
        }

        /// Adds an NOT EXISTS clause to the WHERE statement, specifying a subquery to check the existence of a record.
        /// </summary>
        /// <param name="query">The subquery to be checked for the existence of a record.</param>
        /// <returns>A QueryBase instance for method chaining.</returns>
        public QueryBase NotExists(Query query)
        {
            return this.WriteWhere($"NOT EXISTS {this.RegisterQuery(query)}", AND);
        }

        #endregion

        #region OrWhere
        /// <summary>
        /// Adds an OR condition to the WHERE clause of the query.
        /// </summary>
        public QueryBase OrWhere(ISqlExpressible expressible)
        {
            return this.OrWhere(expressible.ToSafeExpression(this.Info.ToReadOnly(), false));
        }

        /// <summary>
        ///  Adds an OR condition to the WHERE clause of the query.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase OrWhere(SqlExpression expression)
        {
            return this.WriteWhere(this.ToSql(expression), OR);
        }

        /// <summary>
        /// Add a clusule (column=value) to the "WHERE" (If there are any previous clauses, "OR" is entered before the new clause).
        /// </summary>
        /// <param name="column">Column to compare</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBase OrWhere(string column, object value)
        {
            return this.OrWhere(column, value == null ? "IS" : "=", value);
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
            var query = new QueryBase(this.Info.Config);
            callback(query);

            if (query.Info.Where.Parameters.Count > 0)
            {
                query.Info.Where.AddParams(query.Info.Where.Parameters);
                return this.WriteWhere($"({query.Info.Where})", OR);
            }

            return this;
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

            column1 = this.Info.Config.ApplyNomenclature(column1);
            column2 = this.Info.Config.ApplyNomenclature(column2);

            return this.WriteWhere($"{column1} {operation} {column2}", OR);
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
            return this.WriteWhere($"EXISTS {this.RegisterQuery(query)}", OR);
        }

        /// <summary>
        /// Writes a WHERE clause that checks if a subquery returns no rows.
        /// </summary>
        /// <param name="query">The subquery to check.</param>
        public QueryBase OrNotExists(Query query)
        {
            return this.WriteWhere($"NOT EXISTS {this.RegisterQuery(query)}", OR);
        }

        #endregion

        #region Where builder

        private QueryBase WriteBetween(object toCheck, object arg1, object arg2, bool isNot, string whereType)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ParseBetweenArgument(toCheck));
            if (isNot)
                builder.Append(" NOT");

            builder.Append(" BETWEEN ");
            builder.Append(ParseBetweenArgument(arg1));
            builder.Append(" AND ");
            builder.Append(ParseBetweenArgument(arg2));

            return this.WriteWhere(builder.ToString(), whereType);
        }

        private string ParseBetweenArgument(object arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            if (arg is string strColumn)
                return this.Info.Config.ApplyNomenclature(strColumn);

            if (arg is SqlExpression exp)
                return ToSql(exp);

            if (arg is ISqlExpressible expConvert)
                return expConvert.ToSafeExpression(this.Info.ToReadOnly(), false).ToString();

            if (arg is DateTime || arg is TimeSpan || arg.GetType().IsPrimitive)
                return this.RegisterParameterValue(arg);

            throw new InvalidOperationException("The column type is invalid. Use an Expression or string type.");
        }

        private string ToSql(SqlExpression expr)
        {
            this.Info.Where.AddParams(expr.Parameters);

            return expr.ToString();
        }

        internal protected QueryBase WriteWhere(object column, string operation, object value, string type)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            CheckIsAvailableOperation(operation);

            return this.WriteWhere($"{this.ParseColumn(column)} {operation} {this.ParseValue(value)}", type);
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
        protected string ParseColumn(object column)
        {
            if (column is string strColumn)
                return this.Info.Config.ApplyNomenclature(strColumn);

            if (column is ISqlExpressible expConvert)
                column = expConvert.ToExpression(this.Info.ToReadOnly());

            if (column is SqlExpression exp && exp.Parameters.Length == 0)
                return exp.ToString();

            throw new NotSupportedException("The column type is not supported.");
        }

        /// <summary>
        /// Loads the value object and converts it to a sql expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string ParseValue(object value)
        {
            if (value == null)
                return "NULL";

            if (value is ISqlExpressible expConvert)
                value = expConvert.ToExpression(this.Info.ToReadOnly());

            if (value is SqlExpression raw)
                return ToSql(raw);

            return this.RegisterParameterValue(value);
        }

        internal protected QueryBase WriteWhere(string rawSqlExpression, string type)
        {
            if (!this.Info.Where.Empty)
                this.Info.Where.Add($" {type} ");

            this.Info.Where.Add(rawSqlExpression);
            return this;
        }

        private string RegisterParameterValue(object value)
        {
            if (value is Query query)
                return this.RegisterQuery(query);

            if (value is Enum) this.Info.Where.AddParams(Convert.ToInt32(value));
            else this.Info.Where.AddParams(value);

            return "?";
        }

        private string RegisterQuery(Query query)
        {
            this.Info.Where.AddParams(query.Info.Where.Parameters);

            return $"({query})";
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
