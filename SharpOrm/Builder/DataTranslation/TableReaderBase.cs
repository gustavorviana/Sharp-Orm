﻿using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Base class for table translators.
    /// </summary>
    public abstract class TableReaderBase : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();
        protected readonly IQueryConfig config;
        private bool disposed;
        public bool Disposed => this.disposed;

        /// <summary>
        /// Gets the translation registry associated with the table translator.
        /// </summary>
        public static TranslationRegistry Registry { get; set; } = new TranslationRegistry();

        public TableReaderBase(IQueryConfig config)
        {
            this.config = config;
        }

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

            object value = Registry.FromSql(reader[index], reader.GetFieldType(index));
            if (ObjectLoader.CanLoad(value, config))
                value = ObjectLoader.LoadFromDatabase(value, config);

            return new Cell(reader.GetName(index), value);
        }

        public abstract void LoadForeignKeys();

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
        public static Row ToRow(object obj, Type type, bool readForeignKey = false)
        {
            if (obj is Row row)
                return row;

            return new Row(GetTable(type).GetCells(obj, false, readForeignKey).ToArray());
        }

        /// <summary>
        /// Gets the table info for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The table info for the specified type.</returns>
        public static TableInfo GetTable(Type type)
        {
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