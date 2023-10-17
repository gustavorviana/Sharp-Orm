using System;
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
    public abstract class TableReaderBase : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();
        /// <summary>
        /// Gets the translation registry associated with the table translator.
        /// </summary>
        public static TranslationRegistry Registry { get; set; } = new TranslationRegistry();
        protected readonly IQueryConfig config;
        private readonly bool convertToUtc;
        private bool disposed;
        public bool Disposed => this.disposed;

        private DbTransaction transaction;
        private DbConnection connection;

        public TableReaderBase(IQueryConfig config)
        {
            this.convertToUtc = config.DateKind == DateTimeKind.Utc;
            this.config = config;
        }

        public abstract IEnumerable<T> GetEnumerable<T>(DbDataReader reader, CancellationToken token) where T : new();

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from the database reader.
        /// </summary>
        /// <typeparam name="T">The type of the object to parse.</typeparam>
        /// <param name="reader">The database reader.</param>
        /// <returns>The parsed object of type <typeparamref name="T"/>.</returns>
        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
            if (typeof(T) == typeof(Row))
                return (T)(object)GetRow(reader);

            return (T)this.ParseFromReader(typeof(T), reader, "");
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
                return date.FromDatabase(config); ;

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
                return new Query(this.transaction, this.config, name);

            if (this.connection != null)
                return new Query(this.connection, this.config, name);

            return new Query(Connection.ConnectionCreator.Default.GetConnection(), this.config, name);
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
        public static TableInfo GetTable(Type type)
        {
            if (type == typeof(Row))
                return null;

            return cachedTables.GetOrAdd(type, _type => new TableInfo(Registry, _type));
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