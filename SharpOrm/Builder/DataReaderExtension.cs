using SharpOrm.Builder.DataTranslation;
using SharpOrm.Collections;
using SharpOrm.Connection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

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
            return command.SetExpression(new SqlExpression(query, args));
        }

        /// <summary>
        /// Apply SqlExpression to command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="expression">Expression to be executed.</param>
        /// <returns></returns>
        public static DbCommand SetExpression(this DbCommand command, SqlExpression expression)
        {
            command.CommandText = DecodeExpressionString(expression);
            command.Parameters.Clear();

            for (int i = 0; i < expression.Parameters.Length; i++)
                command.AddParam(GetParamName(i + 1), expression.Parameters[i]);

            return command;
        }

        internal static string DecodeExpressionString(SqlExpression expression)
        {
            return expression.ToString().Replace('?', GetParamName);
        }

        internal static string GetParamName(int index)
        {
            return $"@p{index}";
        }

        /// <summary>
        /// Return the rows as a C# object.
        /// </summary>
        /// <typeparam name="T">Type to be converted.</typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExecuteSql<T>(this DbCommand cmd, TranslationRegistry registry = null, CancellationToken token = default, ConnectionManagement management = ConnectionManagement.LeaveOpen)
        {
            return new DbCommandEnumerable<T>(registry, cmd, token, management);
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
            if (obj is DBNull || obj == null)
                return default;

            return (translationRegistry ?? TranslationRegistry.Default).FromSql<T>(obj);
        }

        internal static bool CanCloseConnection(this DbCommand dbCommand, ConnectionManagement management)
        {
            return dbCommand.Transaction is null && CanClose(dbCommand.Connection, management);
        }

        internal static bool CanClose(this DbConnection connection, ConnectionManagement management)
        {
            return management != ConnectionManagement.LeaveOpen && (connection.State == ConnectionState.Open || connection.State == ConnectionState.Connecting);
        }
    }
}
