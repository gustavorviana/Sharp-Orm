using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder
{
    public static class DataReaderExtension
    {
        /// <summary>
        /// Get row of current reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Row ReadRow(this DbDataReader reader, TranslationRegistry translation = null)
        {
            if (translation == null) 
                translation = TranslationRegistry.Default;

            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = GetCell(reader, translation, i);

            return new Row(cells);
        }

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cell GetCell(this DbDataReader reader, TranslationRegistry translation, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), translation.FromSql(reader[index]));
        }

        /// <summary>
        /// Create a new DbCommand with a SQL query and the query's arguments.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query">Query to be executed (to signal an argument, use '?').</param>
        /// <param name="args">Arguments to be used.</param>
        /// <returns></returns>
        public static DbCommand CreateCommand(this DbConnection connection, string query, params object[] args)
        {
            connection.OpenIfNeeded();
            return connection.CreateCommand().SetQuery(query, args);
        }

        /// <summary>
        /// Apply SQL query and argument to command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="query">Query to be executed (to signal an argument, use '?').</param>
        /// <param name="args">Arguments to be used.</param>
        /// <returns></returns>
        public static DbCommand SetQuery(this DbCommand command, string query, params object[] args)
        {
            if (query.Count(c => c == '?') != args.Length)
                throw new InvalidOperationException(Messages.OperationCannotBePerformedArgumentsMismatch);

            command.CommandText = query.Replace('?', p => $"@p{p}");

            for (int i = 0; i < args.Length; i++)
                command.AddParam($"@p{i + 1}", args[i]);

            return command;
        }

        /// <summary>
        /// Return the rows as a C# object.
        /// </summary>
        /// <typeparam name="T">Type to be converted.</typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExecuteSql<T>(this DbCommand cmd)
        {
            using (var reader = new DbObjectReader(new ReadonlyQueryConfig { LoadForeign = true }, cmd.ExecuteReader(), typeof(T)))
                foreach (var item in reader.GetEnumerable<T>())
                    yield return item;
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public static IEnumerable<T> ExecuteArrayScalar<T>(this DbCommand cmd, TranslationRegistry translationRegistry = null)
        {
            ISqlTranslation translation = (translationRegistry ?? TranslationRegistry.Default).GetFor(typeof(T));
            Type expectedType = TranslationRegistry.GetValidTypeFor(typeof(T));

            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    if (reader.IsDBNull(0)) yield return default;
                    else yield return (T)translation.FromSqlValue(reader.GetValue(0), expectedType);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public static T ExecuteScalar<T>(this DbCommand cmd, TranslationRegistry translationRegistry = null)
        {
            var obj = cmd.ExecuteScalar();
            if (obj is DBNull)
                return default;

            return (translationRegistry ?? TranslationRegistry.Default).FromSql<T>(obj);
        }
    }
}
