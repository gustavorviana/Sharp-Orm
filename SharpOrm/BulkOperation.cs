using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Builder.Tables;
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
        private readonly string _targetTable;
        private QueryConfig Config => table.Manager.Config;
        private readonly string[] _tempColumns;
        private readonly CancellationToken _token;
        #endregion

        public BulkOperation(Query target, Row[] tempValues, int? lotInsert)
        {
            if (!target.Info.Config.CanUpdateJoin)
                throw new NotSupportedException(string.Format(Messages.QueryConfigNotSupportOperation, target.Info.Config.GetType()));

            if (tempValues == null || tempValues.Length == 0)
                throw new ArgumentException("tempValues cannot be null or empty.", nameof(tempValues));

            _token = target.Token;
            _tempColumns = tempValues[0].ColumnNames;
            _targetTable = target.Info.TableName.Name;
            var manager = GetValidManager(target.Manager, lotInsert == null || lotInsert == 0, out var isClone);

            try
            {
                table = DbTable.Create(GetSchema(manager), manager);
                InsertTempValues(tempValues, lotInsert);
            }
            catch
            {
                if (isClone)
                    manager?.Dispose();
                table?.Dispose();
                throw;
            }
        }

        private static ConnectionManager GetValidManager(ConnectionManager manager, bool escapeString, out bool isClone)
        {
            if (manager.Management == ConnectionManagement.CloseOnManagerDispose && escapeString == manager.Config.EscapeStrings)
            {
                isClone = false;
                return manager;
            }

            manager = manager.Clone(cloneConfig: true, safeOperations: false);
            manager.Config.EscapeStrings = escapeString;

            manager.Management = ConnectionManagement.CloseOnManagerDispose;
            isClone = true;
            return manager;
        }

        private ITableSchema GetSchema(ConnectionManager manager)
        {
            return new TableBuilder(_targetTable, true)
                .SetBasedTable(_targetTable, _tempColumns)
                .GetSchema();
        }

        private void InsertTempValues(Row[] tempValues, int? lotInsert)
        {
            using (var q = table.GetQuery())
            {
                q.Token = _token;
                if (lotInsert is int lot) q.InsertLot(tempValues, lot);
                else q.BulkInsert(tempValues);
            }
        }

        public int Delete()
        {
            using (var q = GetQuery(_tempColumns))
                return q.Delete();
        }

        public int Update<T>(Expression<ColumnExpression<T>> toCheckColumnsExp)
        {
            using (var targetQuery = GetQueryBase())
            {
                var targetColumns = GetColumns(targetQuery, toCheckColumnsExp);
                if (targetColumns.Length == 0)
                    throw new ArgumentException("No columns found in expression.", nameof(toCheckColumnsExp));

                var toUpdateCells = GetToUpdateCells(targetColumns).ToArray();
                if (toUpdateCells.Length == 0)
                    throw new InvalidOperationException("No columns to update. All columns are used for comparison.");

                targetQuery.Join($"{table.DbName} tempTable", q =>
                {
                    var tempTableColumns = GetColumns((Query)q, toCheckColumnsExp);

                    for (int i = 0; i < tempTableColumns.Length; i++)
                        q.Where(tempTableColumns[i], targetColumns[i]);
                }, "INNER");

                return targetQuery.Update(toUpdateCells.Select(col => GetUpdateCell("tempTable", col)));
            }
        }

        private ExpressionColumn[] GetColumns<T>(Query query, Expression<ColumnExpression<T>> expression, ExpressionConfig config = ExpressionConfig.All)
        {
            var processor = new ExpressionProcessor<T>(query as IFkNodeRoot, config);
            return processor.ParseColumns(expression).ToArray();
        }

        public int Update(string[] comparationColumns)
        {
            if (comparationColumns == null || comparationColumns.Length == 0)
                throw new ArgumentException("comparationColumns cannot be null or empty.", nameof(comparationColumns));

            var toUpdateCells = GetToUpdateCells(comparationColumns);
            if (toUpdateCells.Length == 0)
                throw new InvalidOperationException("No columns to update. All columns are used for comparison.");

            using (var q = GetQuery(comparationColumns))
                return q.Update(toUpdateCells.Select(col => GetUpdateCell("tempTable", col)));
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
            return new Query($"{_targetTable} {TargetAlias}", table.Manager) { Token = _token };
        }

        private string[] GetToUpdateCells(string[] comparationColumns)
        {
            return _tempColumns.Where(x => !comparationColumns.ContainsIgnoreCase(x)).ToArray();
        }

        private IEnumerable<string> GetToUpdateCells(ExpressionColumn[] comparationColumns)
        {
            return _tempColumns.Where(tc => !comparationColumns.Any(c => tc.EqualsIgnoreCase(c.Name)));
        }

        private Cell GetUpdateCell(string tempName, string col)
        {
            return new Cell(
                Config.ApplyNomenclature(ToColumnString("target", col)),
                (SqlExpression)Config.ApplyNomenclature(ToColumnString(tempName, col))
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
                table?.Dispose();
                table?.Manager?.Dispose();
            }

            disposed = true;
        }

        ~BulkOperation()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
