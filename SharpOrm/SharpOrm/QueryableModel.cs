using SharpOrm.Builder;
using System;
using System.Data.Common;

namespace SharpOrm
{
    public abstract class QueryableModel : Model
    {
        protected virtual internal DbConnection Connection { get; set; }
        protected virtual internal IQueryConfig Config { get; set; }

        protected abstract string TableName { get; }
        protected virtual string[] PrimaryKeys { get; set; } = new string[] { "Id" };
        internal bool _isNewModel = true;
        public bool IsNewModel() => this._isNewModel;

        public bool Save(DbTransaction transaction = null, IQueryConfig config = null)
        {
            if (!this.HasChanges())
                return false;

            using (var query = this.Query(transaction: transaction, config: config))
            {
                this.ApplyPrimaryKeys(query);
                if (!this.IsNewModel())
                    return query.Update(this.GetChangedCells());

                query.Insert(this.GetCells());
                return true;
            }
        }

        public bool Delete(DbTransaction transaction = null, IQueryConfig config = null)
        {
            using (var query = this.Query(transaction: transaction, config: config))
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

        public virtual Query Query(string alias = "", DbTransaction transaction = null, IQueryConfig config = null)
        {
            DbConnection connection = this.Connection ?? QueryDefaults.Connection;

            if (config == null)
                config = this.Config ?? QueryDefaults.Config;

            if (transaction == null)
                transaction = QueryDefaults.Transaction;

            if (transaction != null && transaction.Connection != connection)
                transaction = null;

            if (transaction != null)
                return new Query(transaction, config, this.TableName, alias);

            return new Query(connection, config, this.TableName, alias);
        }

        protected ModelQuery<T> GetQuery<T>(string alias = "", DbTransaction transaction = null, IQueryConfig config = null) where T : QueryableModel, new()
        {
            DbConnection connection = this.Connection ?? QueryDefaults.Connection;

            if (config == null)
                config = this.Config ?? QueryDefaults.Config;

            if (transaction == null)
                transaction = QueryDefaults.Transaction;

            if (transaction != null && transaction.Connection != connection)
                transaction = null;

            if (transaction != null)
                return new ModelQuery<T>(transaction, config, this.TableName, alias);

            return new ModelQuery<T>(connection, config, this.TableName, alias);
        }
    }
}
