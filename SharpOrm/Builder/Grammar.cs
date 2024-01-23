using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the base implementation for building SQL queries using a fluent interface.
    /// </summary>
    public abstract class Grammar : IDisposable
    {
        #region Fields\Properties
        public static Action<string> QueryLogger { get; set; }

        private DbCommand _command = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposed = false;
        [Obsolete("It will be removed in version 2.x.x.")]
        protected readonly bool convertToUtc;

        [Obsolete("Use Constructor instead. It will be removed in version 2.x.x.")]
        protected StringBuilder QueryBuilder => this.Constructor.query;
        protected QueryConstructor Constructor { get; }
        protected Query Query { get; }
        public QueryInfo Info => this.Query.Info;

        [Obsolete("It will be removed in version 2.x.x.")]
        protected DbCommand Command => this._command;
        #endregion

        protected Grammar(Query query)
        {
            this.Constructor = new CmdQueryConstructor(query.Info);
            this.convertToUtc = query.Info.Config.DateKind == DateTimeKind.Utc;
            this.Query = query;
            this.Reset();
        }

        #region DML

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public DbCommand Count()
        {
            return this.Count(this.GetColumnToCount());
        }

        private Column GetColumnToCount()
        {
            if (this.Query.Distinct)
                return this.Info.Select.Length == 1 ? this.Info.Select[0] : null;

            if (this.Info.Select.Length > 1 || this.Info.Select.Any(c => c.IsAll()))
                return Column.All;

            return this.Info.Select.FirstOrDefault();
        }

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public DbCommand Count(Column column)
        {
            this.Reset();
            this.ConfigureCount(column);
            return this.BuildCommand();
        }

        /// <summary>
        /// Defines the necessary configuration for count operation.
        /// </summary>
        protected abstract void ConfigureCount(Column column);

        /// <summary>
        /// Generates a SELECT statement and returns a DbCommand object to execute it.
        /// </summary>
        /// <returns>A DbCommand object representing the generated SELECT statement.</returns>
        public DbCommand Select()
        {
            this.Reset();
            this.ConfigureSelect(true);
            return this.BuildCommand();
        }

        /// <summary>
        /// Generates a SELECT statement.
        /// </summary>
        /// <returns></returns>
        public string SelectSqlOnly()
        {
            this.Constructor.Clear();
            this.ConfigureSelect(false);
            return this.Constructor.ToString();
        }

        /// <summary>
        /// This method is an abstract method that must be implemented by any subclass of Grammar.
        /// It is responsible for configuring the SELECT query, including the SELECT statement and WHERE clause, if necessary.
        /// </summary>
        /// <param name="configureWhereParams">Indicates whether to configure the WHERE clause parameters.</param>
        protected abstract void ConfigureSelect(bool configureWhereParams);

        internal DbCommand InsertQuery(QueryBase query, IEnumerable<string> columnNames)
        {
            this.Reset();
            this.ConfigureInsertQuery(query, columnNames);
            return this.BuildCommand();
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected abstract void ConfigureInsertQuery(QueryBase query, IEnumerable<string> columnNames);

        public DbCommand InsertExpression(SqlExpression expression, IEnumerable<string> columnNames)
        {
            this.Reset();
            this.ConfigureInsertExpression(expression, columnNames);
            return this.BuildCommand();
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected abstract void ConfigureInsertExpression(SqlExpression expression, IEnumerable<string> columnNames);

        /// <summary>
        /// Inserts a new record into the database table with the specified cell values.
        /// </summary>
        /// <param name="cells">An array of Cell objects representing the column names and values to be inserted.</param>
        public DbCommand Insert(IEnumerable<Cell> cells)
        {
            this.Reset();
            this.ConfigureInsert(cells, true);
            return this.BuildCommand();
        }

        /// <summary>
        /// Configures the INSERT operation with the given cells to be inserted into the database table, and whether or not to get the generated ID.
        /// </summary>
        /// <param name="cells">The cells to be inserted.</param>
        /// <param name="getGeneratedId">Whether or not to get the generated ID.</param>
        protected abstract void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId);

        /// <summary>
        /// Executes a bulk insert operation with the given rows.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        public DbCommand BulkInsert(IEnumerable<Row> rows)
        {
            this.Reset();
            this.ConfigureBulkInsert(rows);
            return this.BuildCommand();
        }

        /// <summary>
        /// Configures the INSERT statement for inserting multiple rows in a bulk operation.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        protected abstract void ConfigureBulkInsert(IEnumerable<Row> rows);

        /// <summary>
        /// Builds and returns a database command object for executing an update operation based on the specified array of cells.
        /// </summary>
        /// <param name="cells">An array of cells containing the values to be updated.</param>
        public DbCommand Update(IEnumerable<Cell> cells)
        {
            this.Reset();
            this.ConfigureUpdate(cells);
            return this.BuildCommand();
        }

        /// <summary>
        /// This method is used to configure an SQL UPDATE statement with the given cell array.
        /// </summary>
        /// <param name="cells">The cell array to be updated in the table.</param>
        protected abstract void ConfigureUpdate(IEnumerable<Cell> cells);

        /// <summary>
        /// Creates a DELETE command for deleting data from a table.
        /// </summary>
        public DbCommand Delete()
        {
            this.Reset();
            this.ConfigureDelete();
            return this.BuildCommand();
        }

        /// <summary>
        /// This method is an abstract method which will be implemented by the derived classes. It is responsible for configuring a DELETE query command.
        /// </summary>
        protected abstract void ConfigureDelete();

        protected void ApplyDeleteJoins()
        {
            if (!this.CanApplyDeleteJoins())
                return;

            this.Constructor
                .Add(' ')
                .Add(this.TryGetTableAlias(this.Query));

            if (!(this.Query.deleteJoins?.Any() ?? false))
                return;

            foreach (var join in this.Info.Joins.Where(j => this.CanDeleteJoin(j.Info)))
                this.Constructor.Add(", ").Add(this.TryGetTableAlias(join));
        }

        protected virtual bool CanApplyDeleteJoins()
        {
            return this.Info.Joins.Any();
        }

        protected bool CanDeleteJoin(QueryInfo info)
        {
            string name = info.TableName.TryGetAlias(this.Info.Config).ToLower();
            foreach (var jName in this.Query.deleteJoins)
                if (jName.ToLower().Equals(name))
                    return true;

            return false;
        }

        protected virtual void ApplyOrderBy()
        {
            this.ApplyOrderBy(this.Info.Orders, false);
        }

        protected virtual void ApplyOrderBy(IEnumerable<ColumnOrder> order, bool writeOrderByFlag)
        {
            var en = order.GetEnumerator();
            if (!en.MoveNext())
                return;

            if (!writeOrderByFlag)
                this.Constructor.Add(" ORDER BY ");

            WriteOrderBy(en.Current);

            while (en.MoveNext())
            {
                this.Constructor.Add(", ");
                this.WriteOrderBy(en.Current);
            }
        }

        protected void WriteOrderBy(ColumnOrder order)
        {
            if (order.Order == OrderBy.None)
                return;

            this.WriteColumn(order.Column);
            this.Constructor.Add(' ');
            this.Constructor.Add(order.Order);
        }

        protected void WriteColumn(Column column)
        {
            this.Constructor.Add(column.ToExpression(this.Info.ToReadOnly()));
        }

        protected void WriteUpdateCell(Cell cell)
        {
            this.Constructor.Add(this.ApplyTableColumnConfig(cell.Name)).Add(" = ");
            this.Constructor.AddParameter(cell.Value);
        }

        #endregion

        /// <summary>
        /// Resets the grammar object by disposing the current command and creating a new one, clearing the parameters and query builders.
        /// </summary>
        [Obsolete("It will be removed in version 2.x.x.")]
        protected void Reset()
        {
            this.Reset(true);
        }

        [Obsolete("It will be removed in version 2.x.x.")]
        protected void Reset(bool reconfigure)
        {
            if (reconfigure)
                this.Query.Token.ThrowIfCancellationRequested();

            if (this._command != null)
            {
                this._command.Parameters.Clear();
                this._command.Dispose();
            }

            if (reconfigure)
            {
                this._command = this.Query.Connection.CreateCommand();
                this._command.Transaction = this.Query.Transaction;

                this.Query.Token.Register(() =>
                {
                    try { this._command.Cancel(); } catch { }
                });
            }

            this.Constructor.Clear();
        }

        protected string TryGetTableAlias(QueryBase query)
        {
            return query.Info.TableName.TryGetAlias(query.Info.Config);
        }

        protected string GetTableName(bool withAlias)
        {
            return this.GetTableName(this.Query, withAlias);
        }

        protected string GetTableName(QueryBase query, bool withAlias)
        {
            return query.Info.TableName.GetName(withAlias, query.Info.Config);
        }

        private DbCommand BuildCommand()
        {
            this.Query.Token.ThrowIfCancellationRequested();

            this._command.CommandText = this.Constructor.ToString();
            this._command.Transaction = this.Query.Transaction;
            this._command.CommandTimeout = this.Query.CommandTimeout;
            ((CmdQueryConstructor)this.Constructor).ApplyToCommand(this._command);
            QueryLogger?.Invoke(this._command.CommandText);
            return this._command;
        }

        protected virtual void WriteSelectColumns()
        {
            AddParams(this.Info.Select);
        }

        protected void WriteSelect(Column column)
        {
            this.Constructor.AddExpression(column, true);
        }

        protected void AppendCells(IEnumerable<Cell> values)
        {
            AddParams(values, cell => cell.Value);
        }

        protected void AddParams<T>(IEnumerable<T> values, Func<T, object> call = null)
        {
            if (call == null)
                call = obj => obj;

            using (var en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return;

                this.Constructor.AddParameter(call(en.Current));

                while (en.MoveNext())
                    this.Constructor.Add(", ").AddParameter(call(en.Current));
            }
        }

        protected virtual void WriteGroupBy()
        {
            if (this.Info.GroupsBy.Length == 0)
                return;

            this.Constructor.Add(" GROUP BY ");
            AddParams(this.Info.GroupsBy);

            if (this.Info.Having.Empty)
                return;

            this.Constructor
                .Add(" HAVING ")
                .AddAndReplace(
                    Info.Having.ToString(),
                    '?',
                    (count) => this.Constructor.AddParameter(Info.Having.Parameters[count - 1])
                );
        }

        /// <summary>
        /// Apply column prefix and suffix.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected string ApplyTableColumnConfig(string name)
        {
            return this.Info.Config.ApplyNomenclature(name);
        }

        #region IDisposable
        ~Grammar()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            this.Reset(false);
            if (disposing)
                this._command.Dispose();

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
