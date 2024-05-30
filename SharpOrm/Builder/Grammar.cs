using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the base implementation for building SQL queries using a fluent interface.
    /// </summary>
    public abstract class Grammar
    {
        #region Fields\Properties
        public static Action<string> QueryLogger { get; set; }

        protected QueryBuilder builder { get; }
        protected Query Query { get; }
        public QueryInfo Info => this.Query.Info;
        #endregion

        protected Grammar(Query query)
        {
            this.builder = new QueryBuilder(query);
            this.Query = query;
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

        internal SqlExpression InsertQuery(QueryBase query, IEnumerable<string> columnNames)
        {
            return this.BuildExpression(() => this.ConfigureInsertQuery(query, columnNames));
        }

        /// <summary>
        /// Configures the insert query for a given table and columns.
        /// </summary>
        /// <param name="query">The query to be configured.</param>
        /// <param name="columnNames">The names of the columns to be inserted.</param>
        protected abstract void ConfigureInsertQuery(QueryBase query, IEnumerable<string> columnNames);

        public SqlExpression InsertExpression(SqlExpression expression, IEnumerable<string> columnNames)
        {
            return this.BuildExpression(() => this.ConfigureInsertExpression(expression, columnNames));
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
        public SqlExpression Insert(IEnumerable<Cell> cells)
        {
            return this.BuildExpression(() => this.ConfigureInsert(cells, true));
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
            return this.BuildExpression(() => this.ConfigureBulkInsert(rows));
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

        protected void ApplyDeleteJoins()
        {
            if (!this.CanApplyDeleteJoins())
                return;

            this.builder
                .Add(' ')
                .Add(this.TryGetTableAlias(this.Query));

            if (!(this.Query.deleteJoins?.Any() ?? false))
                return;

            foreach (var join in this.Info.Joins.Where(j => this.CanDeleteJoin(j.Info)))
                this.builder.Add(", ").Add(this.TryGetTableAlias(join));
        }

        protected virtual bool CanApplyDeleteJoins()
        {
            return this.Info.Joins.Any();
        }

        protected bool CanDeleteJoin(QueryInfo info)
        {
            string name = info.TableName.TryGetAlias(this.Info.Config);
            foreach (var jName in this.Query.deleteJoins)
                if (jName.Equals(name, StringComparison.CurrentCultureIgnoreCase))
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
                this.builder.Add(" ORDER BY ");

            WriteOrderBy(en.Current);

            while (en.MoveNext())
            {
                this.builder.Add(", ");
                this.WriteOrderBy(en.Current);
            }
        }

        protected void WriteOrderBy(ColumnOrder order)
        {
            if (order.Order == OrderBy.None)
                return;

            this.WriteColumn(order.Column);
            this.builder.Add(' ');
            this.builder.Add(order.Order);
        }

        protected void WriteColumn(Column column)
        {
            this.builder.Add(column.ToExpression(this.Info.ToReadOnly()));
        }

        protected void WriteUpdateCell(Cell cell)
        {
            this.builder.Add(this.ApplyTableColumnConfig(cell.Name)).Add(" = ");
            this.builder.AddParameter(cell.Value);
        }

        #endregion

        protected string TryGetTableAlias(QueryBase query)
        {
            return query.Info.TableName.TryGetAlias(query.Info.Config);
        }

        protected string GetTableName(bool withAlias)
        {
            return this.GetTableName(this.Query, withAlias);
        }

        protected string ApplyNomenclature(string name)
        {
            return this.Info.Config.ApplyNomenclature(name);
        }

        protected string GetTableName(QueryBase query, bool withAlias)
        {
            return query.Info.TableName.GetName(withAlias, query.Info.Config);
        }

        private SqlExpression BuildExpression(Action builderAction)
        {
            this.builder.Clear();
            builderAction();

            return this.builder.ToExpression();
        }

        protected virtual void WriteSelectColumns()
        {
            AddParams(this.Info.Select);
        }

        protected void WriteSelect(Column column)
        {
            this.builder.AddExpression(column, true);
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

                this.builder.AddParameter(call(en.Current));

                while (en.MoveNext())
                    this.builder.Add(", ").AddParameter(call(en.Current));
            }
        }

        protected virtual void WriteGroupBy()
        {
            if (this.Info.GroupsBy.Length == 0)
                return;

            this.builder.Add(" GROUP BY ");
            AddParams(this.Info.GroupsBy);
            if (this.Info.Having.Empty)
                return;

            this.builder
                .Add(" HAVING ")
                .AddAndReplace(
                    Info.Having.ToString(),
                    '?',
                    (count) => this.builder.AddParameter(Info.Having.Parameters[count - 1])
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
    }
}
