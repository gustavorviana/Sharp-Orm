using SharpOrm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Grammars
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
            return Count(GetColumnToCount());
        }

        private Column GetColumnToCount()
        {
            if (Query.Distinct)
                return Info.Select.Length == 1 ? Info.Select[0] : Column.All;

            if (Info.Select.Length > 1 || Info.Select.Any(c => c.IsAll()))
                return Column.All;

            return Info.Select.FirstOrDefault();
        }

        protected void SetParamInterceptor(Func<object, object> func)
        {
            builder.paramInterceptor = func;
        }

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public SqlExpression Count(Column column)
        {
            return BuildExpression(() => ConfigureCount(column));
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
            return BuildExpression(() => ConfigureSelect(true));
        }

        /// <summary>
        /// Generates a SELECT statement.
        /// </summary>
        /// <returns></returns>
        public string SelectSqlOnly()
        {
            builder.Clear();
            ConfigureSelect(false);
            return builder.ToExpression(true, false).ToString();
        }

        /// <summary>
        /// Gets the SQL expression for the SELECT statement.
        /// </summary>
        /// <returns>The SQL expression for the SELECT statement.</returns>
        public SqlExpression GetSelectExpression()
        {
            builder.Clear();
            ConfigureSelect(false);
            return builder.ToExpression();
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
            return BuildExpression(() => ConfigureInsertQuery(query, columnNames));
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected virtual void ConfigureInsertQuery(QueryBase query, string[] columnNames)
        {
            new InsertGrammar(this).BuildInsertQuery(query, columnNames);
        }

        /// <summary>
        /// Builds the insert expression.
        /// </summary>
        /// <param name="expression">The SQL expression.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The SQL expression for the insert.</returns>
        public SqlExpression InsertExpression(SqlExpression expression, string[] columnNames)
        {
            return BuildExpression(() => ConfigureInsertExpression(expression, columnNames));
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected virtual void ConfigureInsertExpression(SqlExpression expression, string[] columnNames)
        {
            new InsertGrammar(this).BuildInsertExpression(expression, columnNames);
        }

        /// <summary>
        /// Inserts a new record into the database table with the specified cell values.
        /// </summary>
        /// <param name="cells">An array of Cell objects representing the column names and values to be inserted.</param>
        public SqlExpression Insert(IEnumerable<Cell> cells, bool returnsInsetionId = true)
        {
            return BuildExpression(() => ConfigureInsert(cells, returnsInsetionId));
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
        public SqlExpression BulkInsert(IEnumerable<Row> rows)
        {
            return BuildExpression(() => ConfigureBulkInsert(rows));
        }

        /// <summary>
        /// Configures the INSERT statement for inserting multiple rows in a bulk operation.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        protected virtual void ConfigureBulkInsert(IEnumerable<Row> rows)
        {
            new InsertGrammar(this).BuildBulkInsert(rows);
        }

        /// <summary>
        /// Builds and returns a database command object for executing an update operation based on the specified array of cells.
        /// </summary>
        /// <param name="cells">An array of cells containing the values to be updated.</param>
        public SqlExpression Update(IEnumerable<Cell> cells)
        {
            return BuildExpression(() => ConfigureUpdate(cells));
        }

        public SqlExpression Upsert(DbName sourceTableName, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            if (whereColumns.Length == 0)
                throw new InvalidOperationException("The comparison columns must be defined.");

            var target = new UpsertQueryInfo(Query.Info.TableName, Query.Info.Config, "Target");
            var source = new UpsertQueryInfo(sourceTableName, Query.Info.Config, "Source");

            return BuildExpression(() => ConfigureUpsert(target, source, whereColumns, updateColumns, insertColumns));
        }


        public SqlExpression Upsert(IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            if (whereColumns.Length == 0)
                throw new InvalidOperationException("The comparison columns must be defined.");

            var target = new UpsertQueryInfo(Query.Info.TableName, Query.Info.Config, "Target");

            return BuildExpression(() => ConfigureUpsert(target, rows, whereColumns, updateColumns));
        }

        protected virtual void ConfigureUpsert(UpsertQueryInfo target, IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            throw new NotSupportedException($"The \"{GetConfigName()}\" configuration does not support upserting rows.");
        }

        protected virtual void ConfigureUpsert(UpsertQueryInfo target, UpsertQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            throw new NotSupportedException($"The \"{GetConfigName()}\" configuration does not support upsert between tables.");
        }

        private string GetConfigName()
        {
            string fullName = Info.Config.GetType().Name;
            if (fullName.EndsWith(nameof(QueryConfig)))
                return fullName.Substring(0, fullName.Length - nameof(QueryConfig).Length);

            return fullName;
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
            return BuildExpression(ConfigureDelete);
        }

        /// <summary>
        /// This method is an abstract method which will be implemented by the derived classes. It is responsible for configuring a DELETE query command.
        /// </summary>
        protected abstract void ConfigureDelete();

        public SqlExpression SoftDelete(SoftDeleteAttribute softDelete)
        {
            return BuildSoftDeleteExpression(ConfigureSoftDelete, softDelete, true);
        }

        protected virtual void ConfigureSoftDelete(SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException("SotDelete is not supported, the object must be configured with the SoftDeleteAttribute attribute.");

            ConfigureUpdate(GetSoftDeleteColumns(softDelete, true));
        }

        public SqlExpression RestoreSoftDeleted(SoftDeleteAttribute softDelete)
        {
            return BuildSoftDeleteExpression(ConfigureRestoreSoftDelete, softDelete, false);
        }

        protected virtual void ConfigureRestoreSoftDelete(SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException("Restore is not supported, the object must be configured with the SoftDeleteAttribute attribute.");

            ConfigureUpdate(GetSoftDeleteColumns(softDelete, false));
        }

        private SqlExpression BuildSoftDeleteExpression(Action<SoftDeleteAttribute> builderAction, SoftDeleteAttribute softDelete, bool isDelete)
        {
            return BuildExpression(() =>
            {
                var originalSoftDelete = Query.Info.Where.softDelete;
                var originalTrashed = Query.Info.Where.Trashed;
                try
                {
                    Query.Info.Where.Trashed = isDelete ? Trashed.Except : Trashed.Only;
                    Query.Info.Where.softDelete = softDelete;
                    builderAction(softDelete);
                }
                finally
                {
                    Query.Info.Where.softDelete = originalSoftDelete;
                    Query.Info.Where.Trashed = originalTrashed;
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

        #endregion

        private SqlExpression BuildExpression(Action builderAction)
        {
            builder.Clear();
            builderAction();

            return builder.ToExpression(true);
        }

        protected QueryBaseInfo GetInfo(QueryBase query)
        {
            return query.Info;
        }
    }
}
