using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using SharpOrm.SqlMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace SharpOrm
{
    internal class BulkOperation : IDisposable
    {
        #region Fields/Properties
        private const string TargetAlias = "target";

        private bool disposed;
        internal readonly DbTable table;
        private readonly string targetTable;
        private QueryConfig Config => this.table.Manager.Config;
        private readonly string[] tempColumns;
        private readonly CancellationToken token;
        #endregion

        public BulkOperation(Query target, Row[] tempValues, int? lotInsert)
        {
            if (!target.Info.Config.CanUpdateJoin)
                throw new NotSupportedException(string.Format(Messages.QueryConfigNotSupportOperation, target.Info.Config.GetType()));

            this.token = target.Token;
            this.tempColumns = tempValues[0].ColumnNames;
            this.targetTable = target.Info.TableName.Name;
            var manager = GetValidManager(target.Manager, lotInsert == null || lotInsert == 0);
            table = DbTable.Create(this.GetSchema(manager), manager);
            this.InsertTempValues(tempValues, lotInsert);
        }

        private static ConnectionManager GetValidManager(ConnectionManager manager, bool escapeString)
        {
            if (manager.Management == ConnectionManagement.CloseOnManagerDispose && escapeString == manager.Config.EscapeStrings)
                return manager;

            manager = manager.Clone(cloneConfig: true, safeOperations: false);
            manager.Config.EscapeStrings = escapeString;

            manager.Management = ConnectionManagement.CloseOnManagerDispose;

            return manager;
        }

        private TableSchema GetSchema(ConnectionManager manager)
        {
            var query = Query.ReadOnly(targetTable, manager.Config);
            if (tempColumns.Length > 0)
                query.Select(tempColumns);

            query.Limit = 0;

            return new TableSchema(string.Concat("temp_", targetTable), query) { Temporary = true };
        }

        private void InsertTempValues(Row[] tempValues, int? lotInsert)
        {
            using (var q = this.table.GetQuery())
            {
                q.Token = token;
                if (lotInsert is int lot) q.InsertLot(tempValues, lot);
                else q.BulkInsert(tempValues);
            }
        }

        public int Delete()
        {
            using (var q = this.GetQuery(this.tempColumns))
                return q.Delete();
        }

        public int Update<T>(Expression<ColumnExpression<T>> toCheckColumnsExp)
        {
            using (var targetQuery = GetQueryBase())
            {
                var targetColumns = GetColumns(targetQuery, toCheckColumnsExp);
                targetQuery.Join($"{table.DbName} tempTable", q =>
                {
                    var tempTableColumns = GetColumns((Query)q, toCheckColumnsExp);

                    for (int i = 0; i < tempTableColumns.Length; i++)
                        q.Where(tempTableColumns[i], targetColumns[i]);
                }, "INNER");

                return targetQuery.Update(GetToUpdateCells(targetColumns).Select(col => GetUpdateCell("tempTable", col)));
            }
        }

        private ExpressionColumn[] GetColumns<T>(Query query, Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            var processor = new ExpressionProcessor<T>(query as IFkNodeRoot, config);
            return processor.ParseColumns(expression).ToArray();
        }

        public int Update(string[] comparationColumns)
        {
            using (var q = this.GetQuery(comparationColumns))
                return q.Update(GetToUpdateCells(comparationColumns).Select(col => GetUpdateCell("tempTable", col)));
        }

        private Query GetQuery(string[] columnNames)
        {
            return GetQueryBase().Join($"{table.DbName} tempTable", q =>
            {
                foreach (var col in columnNames)
                    q.WhereColumn(ToColumnString("tempTable", col), ToColumnString(TargetAlias, col));
            }, "INNER");
        }

        private Query GetQueryBase()
        {
            return new Query($"{this.targetTable} {TargetAlias}", this.table.Manager) { Token = token };
        }

        private string[] GetToUpdateCells(string[] comparationColumns)
        {
            return this.tempColumns.Where(x => !comparationColumns.ContainsIgnoreCase(x)).ToArray();
        }

        private IEnumerable<string> GetToUpdateCells(ExpressionColumn[] comparationColumns)
        {
            return this.tempColumns.Where(tc => !comparationColumns.Any(c => tc.EqualsIgnoreCase(c.Name)));
        }

        private Cell GetUpdateCell(string tempName, string col)
        {
            return new Cell(
                this.Config.ApplyNomenclature(ToColumnString("target", col)),
                (SqlExpression)this.Config.ApplyNomenclature(ToColumnString(tempName, col))
            );
        }

        private static string ToColumnString(string tablePrefix, string columnName)
        {
            return new StringBuilder(tablePrefix).Append('.').Append(columnName).ToString();
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                this.table.Manager.Dispose();
                table.Dispose();
            }

            disposed = true;
        }

        ~BulkOperation()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed)
                return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
