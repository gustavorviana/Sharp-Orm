using System;
using System.Data.Common;

namespace SharpOrm
{
    public abstract class QueryableModel : Model
    {
        protected virtual internal DbConnection Connection { get; set; }
        protected abstract string TableName { get; }
        protected virtual string[] PrimaryKeys { get; set; } = new string[] { "Id" };
        public bool IsNewModel { get; internal set; } = true;

        public bool Save(DbTransaction transaction = null)
        {
            if (!this.HasChanges)
                return false;

            using (var query = this.Query(transaction: transaction))
            {
                this.ApplyPrimaryKeys(query);
                if (!this.IsNewModel)
                    return query.Update(this.GetChangedCells());

                query.Insert(this.GetCells());
                return true;
            }
        }

        public bool Delete(DbTransaction transaction = null)
        {
            using (var query = this.Query(transaction: transaction))
            {
                this.ApplyPrimaryKeys(query);
                return query.Delete();
            }
        }

        protected virtual void ApplyPrimaryKeys(Query query)
        {
            foreach (var column in this.PrimaryKeys)
                query.Where(column, this.GetRawOrDefault(column, null) ?? throw new ArgumentNullException());
        }

        public virtual Query Query(string alias = "", DbTransaction transaction = null)
        {
            DbConnection connection = this.Connection ?? QueryDefaults.Connection;

            if (transaction == null)
                transaction = QueryDefaults.Transaction;

            if (transaction != null && transaction.Connection != connection)
                transaction = null;

            return new Query(connection, this.TableName, alias) { Transaction = transaction };
        }

        protected ModelQuery<T> GetQuery<T>(string alias = "", DbTransaction transaction = null) where T : QueryableModel, new()
        {
            DbConnection connection = this.Connection ?? QueryDefaults.Connection;

            if (transaction == null)
                transaction = QueryDefaults.Transaction;

            if (transaction != null && transaction.Connection != connection)
                transaction = null;

            return new ModelQuery<T>(connection, this.TableName, alias) { Transaction = transaction };
        }
    }
}
