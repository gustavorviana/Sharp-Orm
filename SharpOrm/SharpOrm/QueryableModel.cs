using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace SharpOrm
{
    public abstract class QueryableModel<T> : QueryableModel where T : QueryableModel, new()
    {
        public static ModelQuery<T> ToQuery(string alias = "", DbConnection connection = null, IQueryConfig config = null)
        {
            if (connection == null)
                connection = QueryDefaults.Default.Connection;

            if (config == null)
                config = QueryDefaults.Default.Config;

            return new ModelQuery<T>(connection, config, new T().GetTableName(), alias);
        }

        public static T FindById(int id, DbConnection connection = null, IQueryConfig config = null)
        {
            using (var query = ToQuery("", connection, config))
            {
                query.Where("id", id);
                return query.FirstOrDefault();
            }
        }

        public static T Find(QueryCallback callback, DbConnection connection = null, IQueryConfig config = null)
        {
            using (var query = ToQuery("", connection, config))
            {
                query.Where(callback);
                return query.FirstOrDefault();
            }
        }

        public static T[] All(QueryCallback callback, DbConnection connection = null, IQueryConfig config = null)
        {
            using (var query = ToQuery("", connection, config))
            {
                query.Where(callback);
                return query.All();
            }
        }
    }

    public abstract class QueryableModel : Model
    {
        protected Dictionary<string, object> _cachedValues = new Dictionary<string, object>();

        protected virtual internal DbConnection Connection { get; set; }
        protected virtual internal IQueryConfig Config { get; set; }

        protected virtual string TableName => this.GetType().Name;
        protected virtual string[] PrimaryKeys { get; set; } = new string[] { "Id" };
        internal bool _isNewModel = true;
        public bool IsNewModel() => this._isNewModel;
        public string GetTableName() => this.TableName;

        public virtual void Refresh(string[] columnsToRefresh = null, DbTransaction transaction = null, IQueryConfig config = null)
        {
            using (var query = this.Query(transaction: transaction, config: config))
            {
                this.ApplyPrimaryKeys(query);

                if (columnsToRefresh != null)
                    query.Select(columnsToRefresh);

                using (var reader = query.ExecuteReader())
                    if (reader.Read())
                        this.LoadFromDataReader(reader);

                this._isNewModel = false;
            }
        }

        public virtual bool Save(DbTransaction transaction = null, IQueryConfig config = null)
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

        public virtual bool Delete(DbTransaction transaction = null, IQueryConfig config = null)
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
            DbConnection connection = this.Connection ?? QueryDefaults.Default.Connection;

            if (config == null)
                config = this.Config ?? QueryDefaults.Default.Config;

            if (transaction == null)
                transaction = QueryDefaults.Default.Transaction;

            if (transaction != null && transaction.Connection != connection)
                transaction = null;

            if (transaction != null)
                return new Query(transaction, config, this.TableName, alias);

            return new Query(connection, config, this.TableName, alias);
        }

        protected T Remember<T>(string key, Func<T> func)
        {
            if (this._cachedValues.ContainsKey(key))
                return (T)this._cachedValues[key];

            T obj = func();
            this._cachedValues[key] = obj;
            return obj;
        }

        protected void Forget(string key)
        {
            if (this._cachedValues.ContainsKey(key))
                this._cachedValues.Remove(key);
        }

        public virtual void ReloadProperties()
        {
            this._cachedValues.Clear();

            var props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in props)
            {
                if (!prop.CanRead)
                    continue;

                if (prop.GetValue(this) is QueryableModel model)
                    model.ReloadProperties();
            }
        }
    }
}
