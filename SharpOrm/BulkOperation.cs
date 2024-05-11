﻿using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    internal class BulkOperation : IDisposable
    {
        private bool disposed;
        private readonly DbTable table;
        private readonly string targetTable;
        private QueryConfig Config => this.table.Manager.Config;
        private readonly string[] tempColumns;
        private const string TargetAlias = "target";

        public BulkOperation(ConnectionManager manager, string targetTable, Row[] tempValues, int lotInsert = 100)
        {
            this.targetTable = targetTable;
            this.tempColumns = tempValues[0].ColumnNames;
            table = DbTable.Create(this.GetSchema(manager), manager);
            this.InsertTempValues(tempValues, lotInsert);
        }

        private TableSchema GetSchema(ConnectionManager manager)
        {
            var query = Query.ReadOnly(targetTable, manager.Config);
            if (tempColumns.Length > 0)
                query.Select(tempColumns);

            query.Where(new SqlExpression("0=1"));

            return new TableSchema($"temp_{targetTable}", query) { Temporary = true };
        }

        private void InsertTempValues(Row[] tempValues, int lotInsert)
        {
            using (var q = this.table.GetQuery())
                q.InsertLot(tempValues, lotInsert);
        }

        public int Delete()
        {
            using (var q = new Query(this.targetTable, this.table.Manager))
                return ApplyJoin(q, this.tempColumns).Delete();
        }

        public int Update(string[] comparationColumns)
        {
            using (var q = new Query($"{this.targetTable} {TargetAlias}", this.table.Manager))
            {
                ApplyJoin(q, comparationColumns);
                string tempName = table.Name.TryGetAlias(Config);

                return q.Update(GetToUpdateCells(comparationColumns).Select(col => GetUpdateCell(tempName, col)));
            }
        }

        private string[] GetToUpdateCells(string[] comparationColumns)
        {
            return this.tempColumns.Where(x => !comparationColumns.Contains(x)).ToArray();
        }

        private Cell GetUpdateCell(string tempName, string col)
        {
            return new Cell(
                this.Config.ApplyNomenclature($"target.{col}"), 
                (SqlExpression)this.Config.ApplyNomenclature($"{tempName}.{col}")
            );
        }

        private Query ApplyJoin(Query query, string[] columns)
        {
            return query.Join(table.Name, q =>
            {
                string tempName = table.Name.TryGetAlias(Config);
                foreach (var col in columns)
                    q.WhereColumn($"{tempName}.{col}", $"{TargetAlias}.{col}");
            }, "INNER");
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
