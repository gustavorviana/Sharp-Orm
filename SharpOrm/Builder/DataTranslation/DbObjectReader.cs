using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpOrm.Builder.DataTranslation
{
    public class DbObjectReader
    {
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly DbDataReader reader;
        internal readonly IQueryConfig config;
        private readonly MappedObject map;

        public DbTransaction Transaction { get; set; }
        public DbConnection Connection { get; set; }
        public CancellationToken Token { get; set; }

        /// <summary>
        /// Gets the translation registry associated with the table translator.
        /// </summary>
        private readonly LambdaColumn[] fkToLoad;

        public DbObjectReader(IQueryConfig config, DbDataReader reader, Type type, TranslationRegistry registry) : this(config, reader, type, registry, new LambdaColumn[0])
        {
        }

        public DbObjectReader(IQueryConfig config, DbDataReader reader, Type type, TranslationRegistry registry, LambdaColumn[] fkToLoad)
        {
            this.config = config;
            this.reader = reader;
            this.fkToLoad = fkToLoad;
            this.map = new MappedObject(registry, reader, type);
        }

        public List<T> ReadToEnd<T>() where T : new()
        {
            if (typeof(T) != this.map.type)
                throw new InvalidCastException();

            List<T> list = new List<T>();

            while (!this.Token.IsCancellationRequested && this.reader.Read())
                list.Add((T)this.Read());

            this.Token.ThrowIfCancellationRequested();

            return list;
        }

        public object Read()
        {
            return this.map.Read(this);
        }

        public void LoadForeigns()
        {
            while (!this.Token.IsCancellationRequested && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                info.SetForeignValue(this.GetValueFor(info));
            }

            this.Token.ThrowIfCancellationRequested();
        }

        private object GetValueFor(ForeignInfo info)
        {
            if (info is HasManyInfo manyInfo)
                return this.GetCollectionValueFor(manyInfo);

            using (var query = this.CreateQuery(info))
            {
                query.Limit = 1;

                using (var reader = query.ExecuteReader())
                    return reader.Read() ? this.CreateReaderFor(info, reader).Read() : null;
            }
        }

        private object GetCollectionValueFor(HasManyInfo info)
        {
            using (var query = this.CreateQuery(info))
            {
                IList collection = ReflectionUtils.CreateList(info.Type);

                using (var reader = query.ExecuteReader())
                {
                    var objReader = this.CreateReaderFor(info, reader);
                    while (reader.Read())
                        collection.Add(objReader.Read());
                }

                if (info.Type.IsArray)
                    return ReflectionUtils.ToArray(info.Type.GetElementType(), collection);

                return collection;
            }
        }

        private DbObjectReader CreateReaderFor(ForeignInfo info, DbDataReader reader)
        {
            return new DbObjectReader(this.config, reader, ReflectionUtils.GetGenericArg(info.Type), this.map.registry);
        }

        internal Query CreateQuery(ForeignInfo info)
        {
            var query = this.CreateQuery(info.TableName);
            query.Where(info is HasManyInfo many ? many.LocalKey : "Id", info.ForeignKey);
            return query;
        }

        protected Query CreateQuery(string name)
        {
            if (this.Transaction != null)
                return new Query(this.Transaction, this.config, name) { Token = Token };

            if (this.Connection != null)
                return new Query(this.Connection, this.config, name) { notClose = true, Token = Token };

            return new Query(this.config, name) { Token = Token };
        }

        internal void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            if (this.fkToLoad.FirstOrDefault(f => f.IsSame(column)) is LambdaColumn lCol)
                this.AddFkColumn(lCol, owner, fkValue, column);
            else if (this.config.LoadForeign)
                column.SetRaw(owner, GetObjWithKey(column, fkValue));
        }

        private void AddFkColumn(LambdaColumn lCol, object owner, object fkValue, ColumnInfo column)
        {
            var info = this.foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
            {
                info = column.IsMany ? new HasManyInfo(lCol, fkValue, column.LocalKey) : new ForeignInfo(lCol, fkValue);
                foreignKeyToLoad.Enqueue(info);
            }

            info.AddFkColumn(owner, column);
        }

        private object GetObjWithKey(ColumnInfo column, object fk)
        {
            var fkTable = TableInfo.Get(column.Type);
            object value = fkTable.CreateInstance();
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fk);

            return value;
        }
    }
}
