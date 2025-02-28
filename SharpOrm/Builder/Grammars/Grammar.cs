using SharpOrm;
using SharpOrm.Builder.Grammars.Interfaces;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder.Grammars
{
    /// <summary>
    /// Provides the base implementation for building SQL queries using a fluent interface.
    /// </summary>
    public abstract class Grammar
    {
        protected Func<object, object> ParamInterceptor { get; set; }

        #region Properties
        /// <summary>
        /// Gets the query.
        /// </summary>
        protected Query Query { get; }

        /// <summary>
        /// Gets the query information.
        /// </summary>
        public QueryInfo Info => Query.Info;

        /// <summary>
        /// Gets or sets the action to log the query.
        /// </summary>
        public static Action<string> QueryLogger { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Grammar"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        protected Grammar(Query query)
        {
            Query = query;
        }

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

        /// <summary>
        /// Performs a record count in the database based on the current Query object configuration.
        /// </summary>
        /// <returns>The database command configured to perform the record count.</returns>
        public SqlExpression Count(Column column)
        {
            return BuildExpression(GetSelectGrammar(), x => x.BuildCount(column));
        }

        /// <summary>
        /// Generates a SELECT statement and returns a DbCommand object to execute it.
        /// </summary>
        /// <returns>A DbCommand object representing the generated SELECT statement.</returns>
        public SqlExpression Select(bool configureWhereParams = true)
        {
            return BuildExpression(GetSelectGrammar(), x => x.BuildSelect(configureWhereParams));
        }

        /// <summary>
        /// Generates a SELECT statement.
        /// </summary>
        /// <returns></returns>
        public string SelectSqlOnly()
        {
            var grammar = GetSelectGrammar();
            grammar.Builder.paramInterceptor = ParamInterceptor;

            grammar.BuildSelect(false);
            return grammar.Builder.ToExpression(true, false).ToString();
        }

        protected abstract ISelectGrammar GetSelectGrammar();

        /// <summary>
        /// Builds the insert query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The SQL expression for the insert query.</returns>
        public SqlExpression InsertQuery(QueryBase query, string[] columnNames)
        {
            return BuildExpression(GetInsertGrammar(), x => x.Build(query, columnNames));
        }

        /// <summary>
        /// Builds the insert expression.
        /// </summary>
        /// <param name="expression">The SQL expression.</param>
        /// <param name="columnNames">The column names.</param>
        /// <returns>The SQL expression for the insert.</returns>
        public SqlExpression InsertExpression(SqlExpression expression, string[] columnNames)
        {
            return BuildExpression(GetInsertGrammar(), x => x.Build(expression, columnNames));
        }

        /// <summary>
        /// Inserts a new record into the database table with the specified cell values.
        /// </summary>
        /// <param name="cells">An array of Cell objects representing the column names and values to be inserted.</param>
        public SqlExpression Insert(IEnumerable<Cell> cells, bool returnsInsetionId = true)
        {
            return BuildExpression(GetInsertGrammar(), x => x.Build(cells, returnsInsetionId));
        }

        protected abstract IInsertGrammar GetInsertGrammar();

        /// <summary>
        /// Executes a bulk insert operation with the given rows.
        /// </summary>
        /// <param name="rows">The rows to be inserted.</param>
        public SqlExpression BulkInsert(IEnumerable<Row> rows)
        {
            return BuildExpression(GetBulkInsertGrammar(), x => x.Build(rows));
        }

        protected abstract IBulkInsertGrammar GetBulkInsertGrammar();

        /// <summary>
        /// Builds and returns a database command object for executing an update operation based on the specified array of cells.
        /// </summary>
        /// <param name="cells">An array of cells containing the values to be updated.</param>
        public SqlExpression Update(IEnumerable<Cell> cells)
        {
            return BuildExpression(GetUpdateGrammar(), x => x.Build(cells));
        }

        public SqlExpression SoftDelete(SoftDeleteAttribute softDelete)
        {
            return BuildSoftDeleteExpression(ConfigureSoftDelete, softDelete, true);
        }

        public SqlExpression RestoreSoftDeleted(SoftDeleteAttribute softDelete)
        {
            return BuildSoftDeleteExpression(ConfigureRestoreSoftDelete, softDelete, false);
        }

        private SqlExpression BuildSoftDeleteExpression(Action<IUpdateGrammar, SoftDeleteAttribute> builderAction, SoftDeleteAttribute softDelete, bool isDelete)
        {
            var update = GetUpdateGrammar();
            return BuildExpression(update, (grammar) =>
            {
                var originalSoftDelete = Query.Info.Where.softDelete;
                var originalTrashed = Query.Info.Where.Trashed;
                try
                {
                    Query.Info.Where.Trashed = isDelete ? Trashed.Except : Trashed.Only;
                    Query.Info.Where.softDelete = softDelete;
                    builderAction(grammar, softDelete);
                }
                finally
                {
                    Query.Info.Where.softDelete = originalSoftDelete;
                    Query.Info.Where.Trashed = originalTrashed;
                }
            });
        }

        protected abstract IUpdateGrammar GetUpdateGrammar();

        /// <summary>
        /// Creates a DELETE command for deleting data from a table.
        /// </summary>
        public SqlExpression Delete()
        {
            return BuildExpression(GetDeleteGrammar(), x => x.Build());
        }

        public SqlExpression DeleteIncludingJoins(string[] tables)
        {
            if (tables.Length == 0)
                throw new ArgumentNullException(nameof(tables));

            var deleteNames = tables.Select(GetJoinName).ToArray();

            return BuildExpression(GetDeleteGrammar(), x => x.BuildIncludingJoins(deleteNames));
        }

        private DbName GetJoinName(string name)
        {
            var join = Info
                .Joins
                .FirstOrDefault(
                    x => x
                    .Info
                    .TableName
                    .TryGetAlias()
                    .Equals(name, StringComparison.OrdinalIgnoreCase)
                );

            if (join == null)
                throw new InvalidOperationException(string.Join(Messages.Query.JoinNotFound, name));

            return join.Info.TableName;
        }

        protected abstract IDeleteGrammar GetDeleteGrammar();

        public SqlExpression Upsert(DbName sourceTableName, string[] whereColumns, string[] updateColumns, string[] insertColumns)
        {
            if (whereColumns.Length == 0)
                throw new InvalidOperationException(Messages.Query.ComparisionColumnMustBeSet);

            var target = new UpsertQueryInfo(Query.Info.TableName, Query.Info.Config, "Target");
            var source = new UpsertQueryInfo(sourceTableName, Query.Info.Config, "Source");

            return BuildExpression(GetUpsertGrammar(), x => x.Build(target, source, whereColumns, updateColumns, insertColumns));
        }

        public SqlExpression Upsert(IEnumerable<Row> rows, string[] whereColumns, string[] updateColumns)
        {
            if (whereColumns.Length == 0)
                throw new InvalidOperationException(Messages.Query.ComparisionColumnMustBeSet);

            var target = new UpsertQueryInfo(Query.Info.TableName, Query.Info.Config, "Target");
            return BuildExpression(GetUpsertGrammar(), x => x.Build(target, rows, whereColumns, updateColumns));
        }

        protected abstract IUpsertGrammar GetUpsertGrammar();

        protected void ConfigureSoftDelete(IUpdateGrammar grammar, SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException(string.Join(Messages.Query.SoftNotSupported, "Sot delete"));

            grammar.Build(GetSoftDeleteColumns(softDelete, true));
        }

        protected void ConfigureRestoreSoftDelete(IUpdateGrammar grammar, SoftDeleteAttribute softDelete)
        {
            if (softDelete == null)
                throw new NotSupportedException(message: string.Join(Messages.Query.SoftNotSupported, "Restore"));

            grammar.Build(GetSoftDeleteColumns(softDelete, false));
        }

        private Cell[] GetSoftDeleteColumns(SoftDeleteAttribute softDelete, bool deleted)
        {
            var cells = new Cell[string.IsNullOrEmpty(softDelete.DateColumnName) ? 1 : 2];

            cells[0] = new Cell(softDelete.ColumnName, deleted);
            if (cells.Length == 2)
                cells[1] = new Cell(softDelete.DateColumnName, deleted ? DateTime.UtcNow : (DateTime?)null);

            return cells;
        }

        protected SqlExpression BuildExpression<T>(T grammar, Action<T> action) where T : IGrammarBase
        {
            grammar.Builder.paramInterceptor = ParamInterceptor;

            action(grammar);
            return grammar.Builder.ToExpression(true, true);
        }
    }
}
