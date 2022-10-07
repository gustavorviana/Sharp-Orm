using SharpOrm.Errors;
using System;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder
{
    public class QueryBase : IDisposable
    {
        #region Fields\Const
        internal const string AND = "AND";
        internal const string OR = "OR";

        private bool _disposed = false;
        protected internal readonly QueryInfo info;
        #endregion

        #region Properties
        protected DbCommand Command => info.Command;

        public bool Disposed => this._disposed;
        public bool Distinct { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }

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
            "not between"
        };
        #endregion

        #region QueryBase

        public QueryBase(DbTransaction transaction) : this(transaction.Connection)
        {
            this.Command.Transaction = transaction;
        }

        public QueryBase(DbConnection connection) : this(connection.CreateCommand())
        {

        }

        public QueryBase(DbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            this.info = new QueryInfo(command);
        }

        internal QueryBase(QueryBase query)
        {
            this.info = new QueryInfo(query.info.Command);
            this._disposed = true;
        }

        #endregion

        #region Where
        internal QueryBase WriteWhere(string rawSqlExpression, string type)
        {
            if (this.info.Wheres.Length != 0)
                this.info.Wheres.Append($" {type} ");

            this.info.Wheres.Append(rawSqlExpression);
            return this;
        }

        internal QueryBase WriteWhere(object column, string operation, object value, string type)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!this.AvailableOperations.Contains(operation.ToLower()))
                throw new DatabaseException("Operação SQL inválida: " + operation);

            string rawColumn = column is SqlExpression exp ? ((string)exp) : column?.ToString().RemoveInvalidNameChars();
            string rawValue;

            if (value is SqlExpression valExp) rawValue = (string)valExp;
            else rawValue = this.RegisterParameterValue(value);

            return this.WriteWhere($"{rawColumn} {operation} {rawValue}", type);
        }

        public QueryBase Where(SqlExpression expression)
        {
            return this.WriteWhere((string)expression, AND);
        }

        public QueryBase Where(string column, string operation, object value)
        {
            return this.WriteWhere(column, operation, value, AND);
        }

        public QueryBase Where(QueryCallback callback)
        {
            var query = new QueryBase(this);
            callback(query);

            if (query.info.Wheres.Length > 0)
                return this.WriteWhere($"({query.info.Wheres})", AND);

            return this;
        }
        #endregion

        #region OrWhere
        public QueryBase OrWhere(SqlExpression expression)
        {
            return this.WriteWhere((string)expression, OR);
        }

        public QueryBase OrWhere(string column, string operation, object value)
        {
            return this.WriteWhere(column, operation, value, OR);
        }

        public QueryBase OrWhere(QueryCallback callback)
        {
            var query = new QueryBase(this);
            callback(query);

            if (query.info.Wheres.Length > 0)
                return this.WriteWhere($"({query.info.Wheres})", OR);

            return this;
        }
        #endregion

        public virtual string RegisterParameterValue(object value)
        {
            string name = $"@p{this.Command.Parameters.Count}";
            this.Command.Parameters.Add(this.CreateParameter(name, value));

            return name;
        }

        protected virtual DbParameter CreateParameter(string name, object value)
        {
            var parameter = this.Command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
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

            if (disposing)
                this.info.Command?.Dispose();

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
