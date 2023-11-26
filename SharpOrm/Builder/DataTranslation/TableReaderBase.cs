using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Base class for table translators.
    /// </summary>
    [Obsolete("Use SharpOrm.Builder.DataTranslation instead. It will be removed in version 2.x.x.")]
    public abstract class TableReaderBase : IDisposable
    {
        /// <summary>
        /// Gets the translation registry associated with the table translator.
        /// </summary>
        internal protected List<LambdaColumn> _fkToLoad = new List<LambdaColumn>();
        [Obsolete("Use SharpOrm.Builder.DataTranslation.Default instead. It will be removed in version 2.x.x.")]
        public static TranslationRegistry Registry { get => TranslationRegistry.Default; set => TranslationRegistry.Default = value; }
        protected readonly IQueryConfig config;
        private readonly bool convertToUtc;
        private bool disposed;
        public bool Disposed => this.disposed;

        private DbTransaction transaction;
        private DbConnection connection;

        public CancellationToken Token { get; set; }

        public TableReaderBase(IQueryConfig config)
        {
            this.convertToUtc = config.DateKind == DateTimeKind.Utc;
            this.config = config;
        }

        public abstract IEnumerable<T> GetEnumerable<T>(DbDataReader reader) where T : new();

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from the database reader.
        /// </summary>
        /// <typeparam name="T">The type of the object to parse.</typeparam>
        /// <param name="reader">The database reader.</param>
        /// <returns>The parsed object of type <typeparamref name="T"/>.</returns>
        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
            return (T)this.ParseFromReader(reader, typeof(T));
        }

        public object ParseFromReader(DbDataReader reader, Type type)
        {
            if (type == typeof(Row))
                return GetRow(reader);

            return this.ParseFromReader(type, reader, "");
        }

        /// <summary>
        /// Get row of current reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public Row GetRow(DbDataReader reader)
        {
            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = GetCell(reader, i);

            return new Row(cells);
        }

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Cell GetCell(DbDataReader reader, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), ReadDbObject(reader[index]));
        }

        internal object ReadDbObject(object obj)
        {
            if (this.convertToUtc && obj is DateTime date)
                return date.FromDatabase(config);

            return obj;
        }

        public abstract void LoadForeignKeys();

        public void SetConnection(DbTransaction transaction)
        {
            this.transaction = transaction;
        }

        public void SetConnection(DbConnection connection)
        {
            this.connection = connection;
        }

        protected Query CreateQuery(string name)
        {
            if (this.transaction != null)
                return new Query(this.transaction, this.config, name) { Token = Token };

            if (this.connection != null)
                return new Query(this.connection, this.config, name) { notClose = true, Token = Token };

            return new Query(this.config, name) { Token = Token };
        }
        /// <summary>
        /// Parses an object of the specified <paramref name="typeToParse"/> from the database reader.
        /// </summary>
        /// <param name="typeToParse">The type to parse.</param>
        /// <param name="reader">The database reader.</param>
        /// <param name="prefix">The prefix for column names.</param>
        /// <returns>The parsed object of the specified <paramref name="typeToParse"/>.</returns>
        protected abstract object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix);

        /// <summary>
        /// Converts an object to a row representation.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The row representation of the object.</returns>
        public static Row ToRow(object obj, Type type, bool readPk, bool readFk)
        {
            if (obj is Row row)
                return row;

            return new Row(GetTable(type).GetObjCells(obj, readPk, readFk).ToArray());
        }

        /// <summary>
        /// Gets the table info for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The table info for the specified type.</returns>
        [Obsolete("Use SharpOrm.Builder.GetTable instead. It will be removed in version 2.x.x.")]
        public static TableInfo GetTable(Type type)
        {
            return TableInfo.Get(type);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
        }

        ~TableReaderBase()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().FullName);

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}