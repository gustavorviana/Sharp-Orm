using SharpOrm.Builder.Grammars;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using static SharpOrm.Msg.Messages;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the base implementation for building SQL queries using a fluent interface.
    /// </summary>
    public abstract class Grammar : GrammarBase
    {
        /// <summary>
        /// Gets or sets the action to log the query.
        /// </summary>
        public static Action<string> QueryLogger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grammar"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        protected Grammar(Query query) : base(query)
        {
        }

        #region DML

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public SqlExpression Count()
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

        protected void SetParamInterceptor(Func<object, object> func)
        {
            this.builder.paramInterceptor = func;
        }

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public SqlExpression Count(Column column)
        {
            return this.BuildExpression(() => this.ConfigureCount(column));
        }

        /// <summary>
        /// Defines the necessary configuration for count operation.
        /// </summary>
        protected abstract void ConfigureCount(Column column);

        /// <summary>
        /// Generates a SELECT statement and returns a DbCommand object to execute it.
        /// </summary>
        /// <returns>A DbCommand object representing the generated SELECT statement.</returns>
        public SqlExpression Select()
        {
            return this.BuildExpression(() => this.ConfigureSelect(true));
        }

        /// <summary>
        /// Generates a SELECT statement.
        /// </summary>
        /// <returns></returns>
        public string SelectSqlOnly()
        {
            this.builder.Clear();
            this.ConfigureSelect(false);
            return this.builder.ToString();
        }

        /// <summary>
        /// Gets the SQL expression for the SELECT statement.
        /// </summary>
        /// <returns>The SQL expression for the SELECT statement.</returns>
        public SqlExpression GetSelectExpression()
        {
            this.builder.Clear();
            this.ConfigureSelect(false);
            return this.builder.ToExpression();
        }

        /// <summary>
        /// This method is an abstract method that must be implemented by any subclass of Grammar.
        /// It is responsible for configuring the SELECT query, including the SELECT statement and WHERE clause, if necessary.
        /// </summary>
        /// <param name="configureWhereParams">Indicates whether to configure the WHERE clause parameters.</param>
        protected abstract void ConfigureSelect(bool configureWhereParams);

        /// <summary>
        /// Builds the insert query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The SQL expression for the insert query.</returns>
        internal SqlExpression InsertQuery(QueryBase query, string[] columnNames)
        {
            return this.BuildExpression(() => this.ConfigureInsertQuery(query, columnNames));
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected virtual void ConfigureInsertQuery(QueryBase query, string[] columnNames)
        {
            new InsertGrammar(Query, builder).ConfigureInsertQuery(query, columnNames);
        }

        /// <summary>
        /// Builds the insert expression.
        /// </summary>
        /// <param name="expression">The SQL expression.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The SQL expression for the insert.</returns>
        public SqlExpression InsertExpression(SqlExpression expression, string[] columnNames)
        {
            return this.BuildExpression(() => this.ConfigureInsertExpression(expression, columnNames));
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected virtual void ConfigureInsertExpression(SqlExpression expression, string[] columnNames)
        {
            new InsertGrammar(Query, builder).ConfigureInsertExpression(expression, columnNames);
        }

        /// <summary>
        /// Inserts a new record into the database table with the specified cell values.
        /// </summary>
        /// <param name="cells">An array of Cell objects representing the column names and values to be inserted.</param>
        public SqlExpression Insert(IEnumerable<Cell> cells, bool returnsInsetionId = true)
        {
            return this.BuildExpression(() => this.ConfigureInsert(cells, returnsInsetionId));
        }

        /// <summary>
        /// Configures the INSERT operation with the given cells to be inserted into the database table, and whether or not to get the generated ID.
        /// </summary>
        /// <param name="cells">The cells to be inserted.</param>
        /// <param name="getGeneratedId">Whether or not to get the generated ID.</param>
        protected virtual void ConfigureInsert(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            new InsertGrammar(Query, builder).ConfigureInsert(cells, getGeneratedId);
        }

        /// <summary>
        /// Executes a bulk insert operation with the given rows.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        public SqlExpression BulkInsert(IEnumerable<Row> rows)
        {
            return this.BuildExpression(() => this.ConfigureBulkInsert(rows));
        }

        /// <summary>
        /// Configures the INSERT statement for inserting multiple rows in a bulk operation.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        protected virtual void ConfigureBulkInsert(IEnumerable<Row> rows)
        {
            new InsertGrammar(Query, builder).ConfigureBulkInsert(rows);
        }

        /// <summary>
        /// Builds and returns a database command object for executing an update operation based on the specified array of cells.
        /// </summary>
        /// <param name="cells">An array of cells containing the values to be updated.</param>
        public SqlExpression Update(IEnumerable<Cell> cells)
        {
            return this.BuildExpression(() => this.ConfigureUpdate(cells));
        }



        /// <summary>
        /// This method is used to configure an SQL UPDATE statement with the given cell array.
        /// </summary>
        /// <param name="cells">The cell array to be updated in the table.</param>
        protected abstract void ConfigureUpdate(IEnumerable<Cell> cells);

        /// <summary>
        /// Creates a DELETE command for deleting data from a table.
        /// </summary>
        public SqlExpression Delete()
        {
            return this.BuildExpression(this.ConfigureDelete);
        }

        /// <summary>
        /// This method is an abstract method which will be implemented by the derived classes. It is responsible for configuring a DELETE query command.
        /// </summary>
        protected abstract void ConfigureDelete();

        /// <summary>
        /// Applies the delete joins to the query.
        /// </summary>
        protected void ApplyDeleteJoins()
        {
            if (!this.CanApplyDeleteJoins())
                return;

            this.builder
                .Add(' ')
                .Add(this.TryGetTableAlias(this.Query));

            if (!this.IsMultipleTablesDeleteWithJoin())
                return;

            foreach (var join in this.Info.Joins.Where(j => this.CanDeleteJoin(j.Info)))
                this.builder.Add(", ").Add(this.TryGetTableAlias(join));
        }

        public SqlExpression SoftDelete(SoftDeleteAttribute softDelete)
        {
            return this.BuildSoftDeleteExpression(this.ConfigureSoftDelete, softDelete, true);
        }

        protected virtual void ConfigureSoftDelete(SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException("SotDelete is not supported, the object must be configured with the SoftDeleteAttribute attribute.");

            this.ConfigureUpdate(this.GetSoftDeleteColumns(softDelete, true));
        }

        public SqlExpression RestoreSoftDeleted(SoftDeleteAttribute softDelete)
        {
            return this.BuildSoftDeleteExpression(this.ConfigureRestoreSoftDelete, softDelete, false);
        }

        protected virtual void ConfigureRestoreSoftDelete(SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException("Restore is not supported, the object must be configured with the SoftDeleteAttribute attribute.");

            this.ConfigureUpdate(this.GetSoftDeleteColumns(softDelete, false));
        }

        private SqlExpression BuildSoftDeleteExpression(Action<SoftDeleteAttribute> builderAction, SoftDeleteAttribute softDelete, bool isDelete)
        {
            return this.BuildExpression(() =>
            {
                var originalSoftDelete = this.Query.Info.Where.softDelete;
                var originalTrashed = this.Query.Info.Where.Trashed;
                try
                {
                    this.Query.Info.Where.Trashed = isDelete ? Trashed.Except : Trashed.Only;
                    this.Query.Info.Where.softDelete = softDelete;
                    builderAction(softDelete);
                }
                finally
                {
                    this.Query.Info.Where.softDelete = originalSoftDelete;
                    this.Query.Info.Where.Trashed = originalTrashed;
                }
            });
        }

        private Cell[] GetSoftDeleteColumns(SoftDeleteAttribute softDelete, bool deleted)
        {
            var cells = new Cell[string.IsNullOrEmpty(softDelete.DateColumnName) ? 1 : 2];

            cells[0] = new Cell(softDelete.ColumnName, deleted);
            if (cells.Length == 2)
                cells[1] = new Cell(softDelete.DateColumnName, deleted ? DateTime.UtcNow : (DateTime?)null);

            return cells;
        }

        protected void ThrowDeleteJoinsNotSupported()
        {
            if (this.IsMultipleTablesDeleteWithJoin())
                throw new NotSupportedException("Delete operations on multiple tables with JOINs are not supported in SQL Server. Please execute separate DELETE statements for each table.");
        }

        protected bool IsMultipleTablesDeleteWithJoin()
        {
            return this.Query.deleteJoins?.Any() ?? false;
        }

        /// <summary>
        /// Determines whether delete joins can be applied.
        /// </summary>
        /// <returns>True if delete joins can be applied; otherwise, false.</returns>
        protected virtual bool CanApplyDeleteJoins()
        {
            return this.Info.Joins.Any();
        }

        /// <summary>
        /// Determines whether the join can be deleted.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <returns>True if the join can be deleted; otherwise, false.</returns>
        protected bool CanDeleteJoin(QueryBaseInfo info)
        {
            string name = info.TableName.TryGetAlias(this.Info.Config);
            foreach (var jName in this.Query.deleteJoins)
                if (jName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>
        /// Writes the update cell to the query.
        /// </summary>
        /// <param name="cell">The cell.</param>
        protected void WriteUpdateCell(Cell cell)
        {
            this.builder.Add(this.ApplyTableColumnConfig(cell.Name)).Add(" = ");
            this.builder.AddParameter(cell.Value);
        }

        #endregion

        /// <summary>
        /// Tries to get the table alias for the query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The table alias.</returns>
        protected string TryGetTableAlias(QueryBase query)
        {
            return query.Info.TableName.TryGetAlias(query.Info.Config);
        }

        /// <summary>
        /// Applies the nomenclature to the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name with the applied nomenclature.</returns>
        protected string ApplyNomenclature(string name)
        {
            return this.Info.Config.ApplyNomenclature(name);
        }

        private SqlExpression BuildExpression(Action builderAction)
        {
            this.builder.Clear();
            builderAction();

            return this.builder.ToExpression(true);
        }

        /// <summary>
        /// Writes the select columns to the query.
        /// </summary>
        protected virtual void WriteSelectColumns()
        {
            AddParams(this.Info.Select);
        }

        /// <summary>
        /// Writes the select column to the query.
        /// </summary>
        /// <param name="column">The column.</param>
        protected void WriteSelect(Column column)
        {
            this.builder.AddExpression(column, true);
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

        protected QueryBaseInfo GetInfo(QueryBase query)
        {
            return query.Info;
        }

        protected void ThrowOffsetNotSupported()
        {
            if (this.Query.Offset.HasValue && this.Query.Offset.Value > 0)
                throw new NotSupportedException();
        }

        protected void ThrowLimitNotSupported()
        {
            if (this.Query.Limit.HasValue && this.Query.Limit.Value > 0)
                throw new NotSupportedException(Messages.GrammarMessage.OffsetNotSupported);
        }

        protected void ThrowJoinNotSupported()
        {
            if (this.Query.Info.Joins.Count > 0)
                throw new NotSupportedException(Messages.GrammarMessage.JoinNotSupported);
        }

        protected void ThrowOrderNotSupported()
        {
            if (this.Query.Info.Orders.Length > 0)
                throw new NotSupportedException(Messages.GrammarMessage.OrderByNotSupported);
        }
    }
}
