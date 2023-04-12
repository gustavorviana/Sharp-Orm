using System;
using System.Collections;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public abstract class Grammar : IDisposable
    {
        #region Fields\Properties
        public static bool LogQuery { get; set; }

        private DbCommand _command = null;

        private bool _disposed = false;
        private int whereCount;
        private int valuesCount;

        protected StringBuilder QueryBuilder { get; } = new StringBuilder();
        protected Query Query { get; }
        protected QueryInfo Info => this.Query.Info;
        protected DbCommand Command => this._command;
        #endregion

        public Grammar(Query query)
        {
            this.Query = query;
            this.Reset();
        }

        #region DML

        public DbCommand Count()
        {
            this.Reset();
            this.ConfigureCount();
            return this.BuildCommand();
        }

        protected abstract void ConfigureCount();

        public DbCommand Select(bool configureWhereParams = true)
        {
            this.Reset();
            this.ConfigureSelect(configureWhereParams);
            return this.BuildCommand();
        }

        protected abstract void ConfigureSelect(bool configureWhereParams);

        internal DbCommand InsertQuery(Query query, string[] columnNames)
        {
            this.Reset();

            this.ConfigureInsertQuery(query, columnNames);
            return this.BuildCommand();
        }

        protected abstract void ConfigureInsertQuery(Query query, string[] columnNames);

        public DbCommand Insert(Cell[] cells)
        {
            this.Reset();
            this.ConfigureInsert(cells, true);
            return this.BuildCommand();
        }

        protected abstract void ConfigureInsert(Cell[] cells, bool getGeneratedId);

        public DbCommand BulkInsert(Row[] rows)
        {
            this.Reset();
            this.ConfigureBulkInsert(rows);
            return this.BuildCommand();
        }

        protected abstract void ConfigureBulkInsert(Row[] rows);

        public DbCommand Update(Cell[] cells)
        {
            this.Reset();
            this.ConfigureUpdate(cells);
            return this.BuildCommand();
        }

        protected abstract void ConfigureUpdate(Cell[] cells);

        public DbCommand Delete()
        {
            this.Reset();
            this.ConfigureDelete();
            return this.BuildCommand();
        }

        protected abstract void ConfigureDelete();

        #endregion

        #region Parameters
        protected string RegisterClausuleParameter(object value)
        {
            if (value is ICollection col)
                return string.Format("({0})", this.RegisterCollectionParameters(col));

            this.whereCount++;
            return this.RegisterParameter($"@c{this.whereCount}", value).ParameterName;
        }

        protected string RegisterCollectionParameters(ICollection collection)
        {
            return string.Join(", ", collection.Cast<object>().Select(c => this.RegisterClausuleParameter(c)));
        }

        protected string RegisterCellValue(Cell cell)
        {
            object value = cell.Value;
            if (value is ISqlExpressible expression)
                value = this.ToExpression(expression);

            if (!(value is SqlExpression exp))
                return RegisterValueParam(cell.Value);

            return new StringBuilder()
                .AppendReplaced(
                    exp.ToString(),
                    '?',
                    c => RegisterValueParam(exp.Parameters[c - 1])
                ).ToString();
        }

        private SqlExpression ToExpression(ISqlExpressible expression)
        {
            if (expression is ISqlExpressibleAlias alias)
                return alias.ToExpression(this.Info.ToReadOnly(), false);

            return expression.ToExpression(this.Info.ToReadOnly());
        }

        private string RegisterValueParam(object value)
        {
            this.valuesCount++;
            return this.RegisterParameter($"@v{this.valuesCount}", value).ParameterName;
        }

        protected virtual DbParameter RegisterParameter(string name, object value)
        {
            var p = this.Command.CreateParameter();
            p.ParameterName = name;
            p.Value = Query.Translator.Config.ToSql(value);

            this.Command.Parameters.Add(p);
            return p;
        }
        #endregion

        protected void Reset()
        {
            if (this.Command != null)
            {
                this.Command.Parameters.Clear();
                this.Command.Dispose();
            }

            this._command = this.Query.Connection.CreateCommand();
            this._command.Transaction = this.Query.Transaction;

            this.QueryBuilder.Clear();
            this.whereCount = 0;
            this.valuesCount = 0;
        }

        protected string GetTableName(bool withAlias)
        {
            return this.GetTableName(this.Info, withAlias);
        }

        protected virtual string GetTableName(QueryInfo info, bool withAlias)
        {
            string name = this.ApplyTableColumnConfig(info.From);
            return !withAlias || string.IsNullOrEmpty(info.Alias) ? name : $"{name} {this.ApplyTableColumnConfig(info.Alias)}";
        }

        private DbCommand BuildCommand()
        {
            this.Command.CommandText = this.QueryBuilder.ToString();
            this.Command.Transaction = this.Query.Transaction;
            if (LogQuery)
                System.Diagnostics.Debug.WriteLine(this.Command.CommandText);
            return this.Command;
        }

        protected static QueryInfo GetQueryInfo(Query query)
        {
            return query.Info;
        }

        protected virtual void WriteSelectColumns()
        {
            this.QueryBuilder.Append(string.Join(", ", this.Info.Select.Select(c => c.ToExpression(this.Info.ToReadOnly()))));
        }

        protected virtual void WriteGroupBy()
        {
            if (this.Info.GroupsBy.Length == 0)
                return;

            this.QueryBuilder.AppendFormat(" GROUP BY {0}", string.Join(", ", this.Info.GroupsBy.Select(c => c.ToExpression(this.Info.ToReadOnly()))));
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

            this.Reset();
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
