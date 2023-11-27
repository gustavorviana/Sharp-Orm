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
        [Obsolete("Use Grammar.QueryLogger. It will be removed in version 1.2.5.x.")]
        public static bool LogQuery { get; set; }

        public static Action<string> QueryLogger { get; set; }

        private DbCommand _command = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _disposed = false;
        private readonly ParamWriter whereWriter;
        private readonly ParamWriter valueWriter;
        protected readonly bool convertToUtc;

        protected StringBuilder QueryBuilder { get; } = new StringBuilder();
        protected Query Query { get; }
        public QueryInfo Info => this.Query.Info;
        protected DbCommand Command => this._command;
        #endregion

        protected Grammar(Query query)
        {
            this.convertToUtc = query.Info.Config.DateKind == DateTimeKind.Utc;
            this.whereWriter = new ParamWriter(this, 'c');
            this.valueWriter = new ParamWriter(this, 'v');
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
        /// <param name="configureWhereParams">Indicates whether to include WHERE clause parameters or not.</param>
        /// <returns>A DbCommand object representing the generated SELECT statement.</returns>
        public DbCommand Select(bool configureWhereParams = true)
        {
            this.Reset();
            this.ConfigureSelect(configureWhereParams);
            return this.BuildCommand();
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

            this.QueryBuilder
                .Append(' ')
                .Append(this.TryGetTableAlias(this.Query));

            if (!(this.Query.deleteJoins?.Any() ?? false))
                return;

            foreach (var join in this.Info.Joins.Where(j => this.CanDeleteJoin(j.Info)))
                this.QueryBuilder.Append(',').Append(this.TryGetTableAlias(join));
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

        protected virtual void ApplyOrderBy(IEnumerable<ColumnOrder> order, bool colsOnly)
        {
            var en = order.GetEnumerator();
            if (!en.MoveNext())
                return;

            if (!colsOnly)
                this.QueryBuilder.Append(" ORDER BY ");

            WriteOrderBy(en.Current);

            while (en.MoveNext())
            {
                this.QueryBuilder.Append(',');
                this.WriteOrderBy(en.Current);
            }
        }

        protected void WriteOrderBy(ColumnOrder order)
        {
            if (order.Order == OrderBy.None)
                return;

            this.WriteColumn(order.Column);
            this.QueryBuilder.Append(' ');
            this.QueryBuilder.Append(order.Order);
        }

        protected void WriteColumn(Column column)
        {
            this.QueryBuilder.Append(column.ToExpression(this.Info.ToReadOnly()));
        }

        protected void WriteUpdateCell(Cell cell)
        {
            this.QueryBuilder
                .Append(this.ApplyTableColumnConfig(cell.Name))
                .Append(" = ")
                .Append(this.RegisterCellValue(cell));
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Returns a clause parameter value registered in the WHERE clause of the query.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        /// <returns>The registered clause parameter value.</returns>
        protected string RegisterClausuleParameter(object value)
        {
            return this.whereWriter.LoadValue(value, false);
        }

        /// <summary>
        /// Registers the value of a cell as a parameter in the VALUES clause of an INSERT statement.
        /// </summary>
        /// <param name="cell">Cell to be registered</param>
        /// <returns>Value of the cell registered as parameter</returns>
        protected string RegisterCellValue(Cell cell)
        {
            return this.valueWriter.LoadValue(cell.Value, true);
        }

        internal DbParameter RegisterParameter(string name, object value)
        {
            var p = this.Command.CreateParameter();
            p.ParameterName = name;
            p.Value = ToDbValue(value);

            this.Command.Parameters.Add(p);
            return p;
        }

        private object ToDbValue(object obj)
        {
            if (this.convertToUtc && obj is DateTime date)
                return date.ToDatabase(this.Query.Info.Config);

            return obj;
        }

        #endregion

        /// <summary>
        /// Resets the grammar object by disposing the current command and creating a new one, clearing the parameters and query builders.
        /// </summary>
        protected void Reset()
        {
            this.Reset(true);
        }

        protected void Reset(bool reconfigure)
        {
            if (reconfigure)
                this.Query.Token.ThrowIfCancellationRequested();

            if (this.Command != null)
            {
                this.Command.Parameters.Clear();
                this.Command.Dispose();
            }


            if (reconfigure)
            {
                this._command = this.Query.Connection.CreateCommand();
                this._command.Transaction = this.Query.Transaction;

                this.Query.Token.Register(() =>
                {
                    try { this.Command.Cancel(); } catch { }
                });
            }

            this.QueryBuilder.Clear();
            this.whereWriter.Reset();
            this.valueWriter.Reset();
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

            this.Command.CommandText = this.QueryBuilder.ToString();
            this.Command.Transaction = this.Query.Transaction;
            this.Command.CommandTimeout = this.Query.CommandTimeout;
            QueryLogger?.Invoke(this.Command.CommandText);
            return this.Command;
        }

        protected virtual void WriteSelectColumns()
        {
            this.QueryBuilder.AppendJoin(", ", this.Info.Select.Select(WriteSelect));
        }

        protected string WriteSelect(Column column)
        {
            return this.valueWriter.LoadValue(column, true);
        }

        protected virtual void WriteGroupBy()
        {
            if (this.Info.GroupsBy.Length == 0)
                return;

            this.QueryBuilder.Append(" GROUP BY ").AppendJoin(", ", this.Info.GroupsBy.Select(c => c.ToExpression(this.Info.ToReadOnly())));

            if (this.Info.Having.Empty)
                return;

            this.QueryBuilder
                .Append(" HAVING ")
                .AppendReplaced(
                    Info.Having.ToString(),
                    '?',
                    (count) => this.RegisterClausuleParameter(Info.Having.Parameters[count - 1])
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
            {
                this.Command.Dispose();
            }

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
