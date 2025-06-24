using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    internal class UpsertStrategy
    {
        private readonly DbName _name;
        private readonly int _commandTimeout;
        private readonly QueryConfig _config;
        private readonly CancellationToken _token;
        private readonly ConnectionManager _managerBase;

        private ConnectionManager _manager;
        private Query _query;

        private readonly Row[] _rows;
        private string[] _toCheckColumns;
        private string[] _updateColumns;
        private string[] _insertColumns;
        private bool _excludeInserColumns = false;

        public UpsertStrategy(Query query, params Row[] rows)
        {
            _rows = rows;
            _token = query.Token;
            _config = query.Config;
            _managerBase = query.Manager;
            _name = query.Info.TableName;
            _commandTimeout = query.CommandTimeout;
        }

        public UpsertStrategy SetCheckColumns(params string[] toCheckColumns)
        {
            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            _toCheckColumns = toCheckColumns;
            return this;
        }

        public UpsertStrategy SetUpdateColumns(params string[] updateColumns)
        {
            _updateColumns = updateColumns;
            return this;
        }

        public UpsertStrategy SetInsertColumns(bool exclude, params string[] insertColumns)
        {
            _insertColumns = insertColumns;
            _excludeInserColumns = exclude;
            return this;
        }

        #region Upsert

        /// <summary>
        /// Asynchronously inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public async Task<int> UpsertAsync(CancellationToken token)
        {
            var expression = GetExpression(out var needNonNative);
            try
            {
                foreach (var row in needNonNative)
                    await NonNativeUpsertAsync(row, token);

                if (expression == null)
                    return needNonNative.Count;

                using (var cmd = CreateCommand().AddCancellationToken(token))
                    return await cmd.SetExpressionWithAffectedRowsAsync(expression) + await cmd.ExecuteNonQueryAsync() + needNonNative.Count;
            }
            finally
            {
                CloseTransaction();
            }
        }

        private async Task NonNativeUpsertAsync(Row row, CancellationToken token)
        {
            if (AddWheres(_query, row, _toCheckColumns) && await _query.AnyAsync(token)) await _query.UpdateAsync(row.Cells.Where(x => AnyColumn(_updateColumns, x.Name)), token);
            else await _query.InsertAsync(row.Cells.Where(x => AnyColumn(_insertColumns, x.Name)), token);
        }

        /// <summary>
        /// Inserts or updates multiple rows in the database based on the specified columns to check and update.
        /// </summary>
        /// <param name="rows">The rows to insert or update.</param>
        /// <param name="toCheckColumns">The columns to check for existing records.</param>
        /// <param name="updateColumns">The columns to update if a record exists. If null, all columns will be updated.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rows or toCheckColumns are null or empty.</exception>
        public int Upsert()
        {
            var expression = GetExpression(out var needNonNative);
            try
            {
                foreach (var row in needNonNative)
                    NonNativeUpsert(row);

                if (expression == null)
                    return needNonNative.Count;


                using (var cmd = CreateCommand())
                    return cmd.SetExpressionWithAffectedRows(expression) + cmd.ExecuteNonQuery() + needNonNative.Count;
            }
            finally
            {
                CloseTransaction();
            }
        }

        private void NonNativeUpsert(Row row)
        {
            if (AddWheres(_query, row, _toCheckColumns) && _query.Any()) _query.Update(row.Cells.Where(x => AnyColumn(_updateColumns, x.Name)));
            else _query.Insert(row.Cells.Where(x => AnyColumn(_insertColumns, x.Name)));
        }

        private bool AddWheres(Query query, Row row, string[] toCheckColumns)
        {
            foreach (var column in toCheckColumns)
            {
                if (!row.HasColumn(column))
                    return false;

                query.Where(column, row[column]);
            }

            return true;
        }

        private static bool AnyColumn(string[] columns, string column)
        {
            if (columns == null || columns.Length == 0) return true;

            for (int i = 0; i < columns.Length; i++)
                if (columns[i].Equals(column, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        #endregion

        #region Connection

        private CommandBuilder CreateCommand(bool leaveOpen = false)
        {
            var cmd = _managerBase.GetCommand(_config.Translation, leaveOpen);
            cmd.AddCancellationToken(_token);

            if (_commandTimeout > 0)
                cmd.Timeout = _commandTimeout;

            cmd.LogQuery = true;

            return cmd;
        }

        private SqlExpression GetExpression(out List<Row> needNonNative)
        {
            if (_rows == null || _rows.Length == 0)
            {
                needNonNative = new List<Row>();
                return null;
            }

            ExtractUpsertModes(out var needNative, out needNonNative);
            InitConnection(needNonNative.Count, needNonNative.Count);

            return needNative.Count == 0 ? null : GetGrammar().Upsert(needNative, _toCheckColumns, _updateColumns);
        }

        private void InitConnection(int nativeQtd, int nonNativeQtd)
        {
            bool needTransaction = nativeQtd > 0 && nonNativeQtd > 0 || nonNativeQtd > 1;

            _manager = needTransaction && _managerBase.Transaction == null ? _managerBase.BeginTransaction() : _managerBase;
            _query = new Query(_name, _manager);
        }

        protected internal Grammar GetGrammar()
        {
            return _config.NewGrammar(_query);
        }

        private void CloseTransaction()
        {
            if (_query != null)
            {
                _query.Dispose();
                _query = null;
            }

            if (_managerBase == _manager || _manager == null)
                return;

            _manager.Commit();
            _manager.Dispose();
            _manager = null;
        }

        #endregion

        private void ExtractUpsertModes(out List<Row> needNative, out List<Row> needNonNative)
        {
            _updateColumns = FixUpdateColumns(_rows[0], _toCheckColumns, _updateColumns);
            _insertColumns = FixInsertColumns(_rows[0], _insertColumns, _excludeInserColumns);

            if (!_config.NativeUpsertRows || _insertColumns != null)
            {
                needNonNative = new List<Row>(_rows);
                needNative = new List<Row>();
                return;
            }

            needNonNative = new List<Row>(_rows.Length);
            needNative = new List<Row>(_rows.Length);

            foreach (var row in _rows)
                if (!HasCheckColumns(row, _toCheckColumns)) needNonNative.Add(row);
                else needNative.Add(row);
        }

        private bool HasCheckColumns(Row row, string[] toCheckColumns)
        {
            if (toCheckColumns == null || toCheckColumns.Length == 0)
                return false;

            return toCheckColumns.All(row.HasColumn);
        }

        private string[] FixInsertColumns(Row row, string[] toInsert, bool excludeColumns)
        {
            if (toInsert == null)
                return null;

            if (toInsert.Length == 0)
                return row.Cells.Select(x => x.Name).ToArray();

            if (!excludeColumns)
                return toInsert;

            return row.Where(cell => !toInsert.Contains(cell.Name, StringComparer.OrdinalIgnoreCase)).Select(x => x.Name).ToArray();
        }

        private string[] FixUpdateColumns(Row row, string[] toCheckColumns, string[] updateColumns)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row), "Rows cannot be null or empty.");

            if (toCheckColumns == null || toCheckColumns.Length == 0)
                throw new ArgumentNullException(nameof(toCheckColumns), "ToCheckColumns cannot be null or empty.");

            if (updateColumns == null || updateColumns.Length == 0)
                updateColumns = row.Cells.Select(x => x.Name).Where(x => !toCheckColumns.ContainsIgnoreCase(x)).ToArray();

            if (updateColumns.Length == 0)
                return row.Cells.Select(x => x.Name).ToArray();

            return updateColumns;
        }
    }
}
