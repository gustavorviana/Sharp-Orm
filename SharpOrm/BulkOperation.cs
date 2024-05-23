using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Linq;
using System.Threading;

namespace SharpOrm
{
    internal class BulkOperation : IDisposable
    {
        #region Fields/Properties
        private const string TargetAlias = "target";

        private bool disposed;
        private readonly DbTable table;
        private readonly string targetTable;
        private QueryConfig Config => this.table.Manager.Config;
        private readonly string[] tempColumns;
        private readonly CancellationToken token;
        #endregion

        public BulkOperation(Query target, Row[] tempValues, int lotInsert = 100)
        {
            if (!target.Info.Config.CanUpdateJoin)
                throw new NotSupportedException($"{target.Info.Config.GetType()} does not support this operation.");

            this.token = target.Token;
            this.tempColumns = tempValues[0].ColumnNames;
            this.targetTable = target.Info.TableName.Name;
            table = DbTable.Create(this.GetSchema(target.Manager), target.Manager);
            this.InsertTempValues(tempValues, lotInsert);
        }

        private TableSchema GetSchema(ConnectionManager manager)
        {
            var query = Query.ReadOnly(targetTable, manager.Config);
            if (tempColumns.Length > 0)
                query.Select(tempColumns);

            query.Limit = 0;

            return new TableSchema(string.Concat("temp_", targetTable), query) { Temporary = true };
        }

        private void InsertTempValues(Row[] tempValues, int lotInsert)
        {
            using (var q = this.table.GetQuery())
            {
                q.Token = token;
                q.InsertLot(tempValues, lotInsert);
            }
        }

        public int Delete()
        {
            using (var q = this.GetQuery(this.tempColumns))
                return q.Delete();
        }

        public int Update(string[] comparationColumns)
        {
            using (var q = this.GetQuery(comparationColumns))
            {
                string tempName = table.Name.TryGetAlias(Config);
                return q.Update(GetToUpdateCells(comparationColumns).Select(col => GetUpdateCell(tempName, col)));
            }
        }

        private Query GetQuery(string[] comparationColumns)
        {
            return ApplyJoin(new Query(string.Concat(this.targetTable, " ", TargetAlias), this.table.Manager) { Token = token }, comparationColumns);
        }

        private Query ApplyJoin(Query query, string[] columns)
        {
            return query.Join(table.Name, q =>
            {
                string tempName = table.Name.TryGetAlias(Config);
                foreach (var col in columns)
                    q.WhereColumn(string.Concat(tempName, ".", col), string.Concat(TargetAlias, ".", col));
            }, "INNER");
        }

        private string[] GetToUpdateCells(string[] comparationColumns)
        {
            return this.tempColumns.Where(x => !comparationColumns.Contains(x)).ToArray();
        }

        private Cell GetUpdateCell(string tempName, string col)
        {
            return new Cell(
                this.Config.ApplyNomenclature(string.Concat("target.", col)),
                (SqlExpression)this.Config.ApplyNomenclature(string.Concat(tempName, ".", col))
            );
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                table.Dispose();

            disposed = true;
        }

        ~BulkOperation()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (!this.disposed)
                return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
