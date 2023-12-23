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
#pragma warning disable CS0618 // O tipo ou membro é obsoleto
    /// <summary>
    /// Object responsible for reading and managing the DbDataReader.
    /// </summary>
    public class DbObjectReader : IDisposable
    {
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly DbDataReader reader;
        internal readonly IQueryConfig config;
        private readonly MappedObject map;
        private bool disposed;

        public DbDataReader Reader => this.reader;

        public DbTransaction Transaction { get; set; }
        public DbConnection Connection { get; set; }
        public CancellationToken Token { get; set; }

        private LambdaColumn[] _fkToLoad = new LambdaColumn[0];
        /// <summary>
        /// Foreign keys to be loaded.
        /// </summary>
        public LambdaColumn[] FkToLoad { get => this._fkToLoad; set => this._fkToLoad = value ?? throw new ArgumentNullException(nameof(this.FkToLoad)); }

        public DbObjectReader(IQueryConfig config, DbDataReader reader, Type type)
            : this(config, reader, type, TranslationRegistry.Default)
        {

        }

        public DbObjectReader(IQueryConfig config, DbDataReader reader, Type type, TranslationRegistry registry)
            : this(config, reader, MappedObject.Create(reader, type, registry), registry)
        {
        }

        public DbObjectReader(IQueryConfig config, DbDataReader reader, MappedObject map, TranslationRegistry registry = null)
        {
            this.config = config;
            this.reader = reader;
            this.map = map;
        }

        /// <summary>
        /// Reads all available objects in the DbDataReader and their foreign objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public List<T> ReadToEnd<T>() where T : new()
        {
            List<T> list = new List<T>(this.GetEnumerable<T>());

            if (!this.reader.IsClosed)
                this.reader.Close();

            this.LoadForeigns();

            return list;
        }

        /// <summary>
        /// Returns an IEnumerable that reads all objects from the DbDataReader without reading the foreign keys.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public IEnumerable<T> GetEnumerable<T>()
        {
            while (this.MoveNext())
                yield return this.Read<T>();
        }

        public bool MoveNext()
        {
            this.Token.ThrowIfCancellationRequested();
            return this.reader.Read();
        }

        public T Read<T>()
        {
            return (T)this.Read();
        }

        /// <summary>
        /// Reads the object at the current position of the reader.
        /// </summary>
        /// <returns></returns>
        public object Read()
        {
            return this.map.Read(this.reader, this);
        }

        /// <summary>
        /// Reads the foreign keys of the objects.
        /// </summary>
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

                using (var reader = this.CreateReaderFor(info, query.ExecuteReader()))
                    return reader.MoveNext() ? reader.Read() : null;
            }
        }

        private object GetCollectionValueFor(HasManyInfo info)
        {
            using (var query = this.CreateQuery(info))
            {
                IList collection = ReflectionUtils.CreateList(info.Type);

                using (var reader = this.CreateReaderFor(info, query.ExecuteReader()))
                    while (reader.MoveNext())
                        collection.Add(reader.Read());

                if (info.Type.IsArray)
                    return ReflectionUtils.ToArray(info.Type.GetElementType(), collection);

                return collection;
            }
        }

        private DbObjectReader CreateReaderFor(ForeignInfo info, DbDataReader reader)
        {
            return new DbObjectReader(this.config, reader, ReflectionUtils.GetGenericArg(info.Type));
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

            if (this.FkToLoad.FirstOrDefault(f => f.IsSame(column)) is LambdaColumn lCol)
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

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                this.reader.Dispose();

            disposed = true;
        }

        ~DbObjectReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().FullName);

            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
}
