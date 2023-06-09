using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Base class for table translators.
    /// </summary>
    public abstract class TableTranslatorBase
    {
        private static readonly ConcurrentDictionary<Type, TableInfo> cachedTables = new ConcurrentDictionary<Type, TableInfo>();

        /// <summary>
        /// Gets the translation registry associated with the table translator.
        /// </summary>
        public static TranslationRegistry Registry { get; set; } = new TranslationRegistry();

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from the database reader.
        /// </summary>
        /// <typeparam name="T">The type of the object to parse.</typeparam>
        /// <param name="reader">The database reader.</param>
        /// <returns>The parsed object of type <typeparamref name="T"/>.</returns>
        public T ParseFromReader<T>(DbDataReader reader) where T : new()
        {
            if (typeof(T) == typeof(Row))
                return (T)(object)reader.GetRow(Registry);

            return (T)this.ParseFromReader(typeof(T), reader, "");
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
        public static Row ToRow(object obj, Type type)
        {
            if (obj is Row row)
                return row;

            return new Row(GetTable(type).GetCells(obj).ToArray());
        }

        /// <summary>
        /// Gets the table name of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The table name of the specified type.</returns>
        public static string GetTableNameOf(Type type)
        {
            return GetTable(type).Name;
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
    }
}
