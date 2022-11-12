using SharpOrm.Errors;
using System;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// SQL clause creator
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
        protected virtual string[] AvailableOperations { get; } = {
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
            "between",
            "not in",
            "not like",
            "not between",
            "is",
            "is not"
        };

        public QueryBase(IQueryConfig config)
        {
            this.Info = new QueryInfo(config);
        }

        #region Where
        internal protected QueryBase WriteWhere(string rawSqlExpression, string type)
        {
            if (this.Info.Wheres.Length != 0)
                this.Info.Wheres.Append($" {type} ");

            this.Info.Wheres.Append(rawSqlExpression);
            return this;
        }

        internal protected QueryBase WriteWhere(object column, string operation, object value, string type)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            this.CheckIsAvailableOperation(operation);

            return this.WriteWhere($"{this.ParseColumn(column)} {operation} {this.ParseValue(value)}", type);
        }

        /// <summary>
        /// Checks whether the inserted operation has been recorded in "this.AvailableOperations".
        /// </summary>
        /// <param name="operation"></param>
        protected void CheckIsAvailableOperation(string operation)
        {
            if (!this.AvailableOperations.Contains(operation.ToLower()))
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

            if (column is SqlExpression exp)
                return exp.ToString();

            if (column is IExpressionConversion expConvert)
                return expConvert.ToExpression(this).ToString();

            throw new InvalidOperationException("The column type is invalid. Use an Expression or string type.");
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

            if (value is SqlExpression raw)
                return raw.ToString();

            if (value is Column column)
                return column.ToExpression(this).ToString();

            return this.RegisterParameterValue(value);
        }

        /// <summary>
        /// Adds the sql clause to the "WHERE" (If there are any previous clauses, "AND" is inserted before the new clause)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase Where(SqlExpression expression)
        {
            return this.WriteWhere((string)expression, AND);
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

            if (query.Info.Wheres.Length > 0)
            {
                this.Info.WhereObjs.AddRange(query.Info.WhereObjs);
                return this.WriteWhere($"({query.Info.Wheres})", AND);
            }

            return this;
        }

        /// <summary>
        /// adds a value comparison clause between columns in "WHERE"
        /// </summary>
        /// <param name="column1">Column 1 to compare value.</param>
        /// <param name="operation">Operation</param>
        /// <param name="column2">Column 2 to compare value.</param>
        /// <returns></returns>
        public QueryBase WhereColumn(string column1, string operation, string column2)
        {
            this.CheckIsAvailableOperation(operation);

            column1 = this.Info.Config.ApplyNomenclature(column1);
            column2 = this.Info.Config.ApplyNomenclature(column2);

            return this.WriteWhere($"{column1} {operation} {column2}", AND);
        }

        #endregion

        #region OrWhere
        /// <summary>
        /// Adds the sql clause to the "WHERE" (If there are any previous clauses, "OR" is inserted before the new clause)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryBase OrWhere(SqlExpression expression)
        {
            return this.WriteWhere((string)expression, OR);
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

            if (query.Info.Wheres.Length > 0)
            {
                this.Info.WhereObjs.AddRange(query.Info.WhereObjs);
                return this.WriteWhere($"({query.Info.Wheres})", OR);
            }

            return this;
        }

        /// <summary>
        /// adds a value comparison clause between columns in "WHERE"
        /// </summary>
        /// <param name="column1">Column 1 to compare value.</param>
        /// <param name="operation">Operation</param>
        /// <param name="column2">Column 2 to compare value.</param>
        /// <returns></returns>
        public QueryBase OrWhereColumn(string column1, string operation, string column2)
        {
            this.CheckIsAvailableOperation(operation);

            column1 = this.Info.Config.ApplyNomenclature(column1);
            column2 = this.Info.Config.ApplyNomenclature(column2);

            return this.WriteWhere($"{column1} {operation} {column2}", OR);
        }

        #endregion

        private string RegisterParameterValue(object value)
        {
            if (value is Query query)
                return this.RegisterQuery(query);

            this.Info.WhereObjs.Add(value);
            return "?";
        }

        private string RegisterQuery(Query query)
        {
            this.Info.WhereObjs.AddRange(query.Info.WhereObjs);

            return $"({query})";
        }

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
