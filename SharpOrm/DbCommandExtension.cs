using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm
{
    public static class DbCommandExtension
    {
        /// <summary>
        /// Return the rows as a C# object.
        /// </summary>
        /// <typeparam name="T">Type to be converted.</typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExecuteEnumerable<T>(this DbCommand cmd, TranslationRegistry registry = null, CancellationToken token = default, ConnectionManagement management = ConnectionManagement.CloseOnEndOperation)
        {
            return new DbCommandEnumerable<T>(cmd, registry, management, token);
        }

        /// <summary>
        /// Sets the cancellation token for the specified database command.
        /// </summary>
        /// <param name="command">The database command to which the cancellation token will be assigned.</param>
        /// <param name="token">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>The database command with the cancellation token assigned.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token has already been canceled.</exception>
        public static DbCommand SetCancellationToken(this DbCommand command, CancellationToken token)
        {
            if (token == default) return command;

            token.ThrowIfCancellationRequested();
            CancellationTokenRegistration registry = default;
            registry = token.Register(() =>
            {
                try { command.Cancel(); } catch { }
                registry.Dispose();
            });

            command.Disposed += (sender, e) => registry.Dispose();

            return command;
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        public static IEnumerable<T> ExecuteArrayScalar<T>(this DbCommand cmd, TranslationRegistry translationRegistry = null, ConnectionManagement management = ConnectionManagement.LeaveOpen)
        {
            ISqlTranslation translation = (translationRegistry ?? TranslationRegistry.Default).GetFor(typeof(T));
            Type expectedType = TranslationRegistry.GetValidTypeFor(typeof(T));

            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    if (reader.IsDBNull(0)) yield return default;
                    else yield return (T)translation.FromSqlValue(reader.GetValue(0), expectedType);

            if (CanClose(management)) cmd.Connection.Close();
        }

        internal static bool CanClose(ConnectionManagement management)
        {
            return management != ConnectionManagement.LeaveOpen && management != ConnectionManagement.CloseOnManagerDispose;
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public static T ExecuteScalar<T>(this DbCommand cmd, TranslationRegistry translationRegistry = null)
        {
            return (translationRegistry ?? TranslationRegistry.Default).FromSql<T>(cmd.ExecuteScalar());
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
                command.AddParam(expression.GetParamName(i + 1), expression.Parameters[i]);

            return command;
        }

        internal static string DecodeExpressionString(SqlExpression expression)
        {
            return expression.ToString().Replace('?', expression.GetParamName);
        }
    }
}
