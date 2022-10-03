using SharpOrm.Errors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryBase : IDisposable
    {
        #region Fields
        private bool _disposed = false;
        protected internal readonly QueryInfo info;
        #endregion

        #region Properties
        protected DbCommand Command => info.Command;

        public bool Disposed => this._disposed;
        public bool Distinct { get; set; }
        public string From { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string SafeFrom => this.From.AlphaNumericOnly(' ');

        public virtual string[] AvailableOperations { get; } = {
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
            this.info = new QueryInfo(command);
        }

        internal QueryBase(QueryBase query)
        {
            this.info = new QueryInfo(query.info.Command);
            this._disposed = true;
        }

        #endregion

        #region Protected Write Expression

        protected void WriteExpression(StringBuilder builder, SqlExpression expression, string type)
        {
            if (builder.Length != 0)
                builder.Append($" {type} ");

            builder.Append(expression);
        }

        protected virtual void WriteExpression(StringBuilder builder, object column, string operation, object value, string type)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!this.AvailableOperations.Contains(operation.ToLower()))
                throw new DatabaseException("Operação SQL inválida: " + operation);

            string rawColumn = column is SqlExpression exp ? ((string)exp) : column?.ToString().RemoveInvalidNameChars();
            string rawValue;

            if (value is SqlExpression valExp) rawValue = (string)valExp;
            else this.RegisterParameterValue(out rawValue, value);

            this.WriteExpression(builder, new SqlExpression($"{rawColumn} {operation} {rawValue}"), type);
        }
        #endregion

        #region Where
        public void Where(string name, string operation, object value, string type)
        {
            this.WriteExpression(this.info.Wheres, name, operation, value, type);
        }

        public void Where(Column column, string operation, object value, string type)
        {
            this.WriteExpression(this.info.Wheres, column, operation, value, type);
        }

        public void Where(SqlExpression expression, string type)
        {
            this.WriteExpression(this.info.Wheres, expression, type);
        }
        #endregion

        public virtual void RegisterParameterValue(out string name, object value)
        {
            name = $"@p{this.Command.Parameters.Count}";
            this.Command.Parameters.Add(this.CreateParameter(name, value));
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
