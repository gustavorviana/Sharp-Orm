using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm
{
    public class ModelQuery<T> : Query where T : Model, new()
    {
        public ModelQuery(string table, string alias = "") : base(table, alias)
        {
        }

        public ModelQuery(DbConnection connection, string table, string alias = "") : base(connection, table, alias)
        {
        }

        public ModelQuery(DbConnection connection, IQueryConfig config, string table, string alias = "") : base(connection, config, table, alias)
        {
        }

        #region DML

        public T FirstOrDefault()
        {
            using (var query = this.Clone(true))
            {
                query.Limit = 1;
                using (var reader = query.ExecuteReader())
                {
                    return reader.Read() ? this.FromDataReader(reader) : null;
                }
            }
        }

        public T[] All()
        {
            List<T> models = new List<T>();

            using (var reader = this.ExecuteReader())
                while (reader.Read())
                    models.Add(FromDataReader(reader));

            return models.ToArray();
        }

        private T FromDataReader(DbDataReader reader)
        {
            T model = new T();

            for (int i = 0; i < reader.FieldCount; i++)
                this.AddColumn(reader, model, i);

            if (model is QueryableModel qm)
                qm.Connection = this.Connection;

            return model;
        }

        private void AddColumn(DbDataReader reader, T model, int index)
        {
            model.columns[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader[index];
        }

        /// <summary>
        /// Update rows on table.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public bool Update(T model)
        {
            if (model == null)
                throw new ArgumentException();

            return base.Update(model.GetCells());
        }

        /// <summary>
        /// Inserts one row into the table.
        /// </summary>
        /// <param name="cells"></param>
        public void Insert(T model)
        {
            if (model == null)
                throw new ArgumentException();

            base.Insert(model.GetCells());
        }

        /// <summary>
        /// Inserts one or more rows into the table.
        /// </summary>
        /// <param name="rows"></param>
        public void BulkInsert(params T[] models)
        {
            base.BulkInsert(models.Select(m => new Row(m.GetCells())).ToArray());
        }

        #endregion

        public override Query Clone(bool withWhere)
        {
            ModelQuery<T> query = new ModelQuery<T>(this.Connection, this.Config, this.info.From, this.info.Alias);
            if (withWhere)
                query.info.LoadFrom(this.info);

            return query;
        }
    }
}
