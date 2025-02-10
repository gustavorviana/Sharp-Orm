using SharpOrm.Collections;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.Connection
{
    public static class ConnectionUtils
    {
        /// <summary>
        /// Opens the database connection if it is not already open.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>The opened database connection.</returns>
        /// <exception cref="Errors.DbConnectionException">Thrown when there is an error opening the connection.</exception>
        public static DbConnection OpenIfNeeded(this DbConnection connection)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                return connection;
            }
            catch (Exception ex)
            {
                throw new Errors.DbConnectionException(ex);
            }
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an array of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned array.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>An array of type <typeparamref name="T"/> representing the query results.</returns>

        public static T[] ExecuteArray<T>(this ConnectionManager manager, string sql, TranslationRegistry registry = null, CancellationToken token = default)
        {
            return ExecuteArray<T>(manager, new SqlExpression(sql), registry, token);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an array of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned array.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>An array of type <typeparamref name="T"/> representing the query results.</returns>
        public static T[] ExecuteArray<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry = null, CancellationToken token = default)
        {
            return ExecuteEnumerable<T>(manager, expression, registry, token).ToArray();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an enumerable collection of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned collection.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="registry">Optional. The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> representing the query results.</returns>
        public static IEnumerable<T> ExecuteEnumerable<T>(this ConnectionManager manager, string sql, TranslationRegistry registry = null, CancellationToken token = default)
        {
            return ExecuteEnumerable<T>(manager, new SqlExpression(sql), registry, token);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an enumerable collection of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned collection.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">Optional. The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> representing the query results.</returns>
        public static IEnumerable<T> ExecuteEnumerable<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry = null, CancellationToken token = default)
        {
            return new DbCommandEnumerable<T>(manager.CreateCommand(expression).SetCancellationToken(token), registry, manager.Management, token)
            {
                manager = manager
            };
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the number of rows affected.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The number of rows affected by the SQL query.</returns>
        public static int ExecuteNonQuery(this ConnectionManager manager, string sql, CancellationToken token = default)
        {
            return ExecuteNonQuery(manager, new SqlExpression(sql), token);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the number of rows affected.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The number of rows affected by the SQL query.</returns>
        public static int ExecuteNonQuery(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            try
            {
                using (var cmd = manager.CreateCommand(expression).SetCancellationToken(token))
                    return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        public static T ExecuteScalar<T>(this ConnectionManager manager, string sql, CancellationToken token = default)
        {
            return ExecuteScalar<T>(manager, new SqlExpression(sql), token);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <param name="expression">SqlExpression to execute.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public static T ExecuteScalar<T>(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            try
            {
                using (var cmd = manager.CreateCommand(expression).SetCancellationToken(token))
                    return cmd.ExecuteScalar<T>();
            }
            catch(Exception ex)
            {
                token.ThrowIfCancellationRequested();
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Creates a new database command with the default command timeout.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <returns>The created database command.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager, SqlExpression expression)
        {
            return CreateCommand(manager, manager.CommandTimeout).SetExpression(expression);
        }

        /// <summary>
        /// Creates a new database command with the default command timeout.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <returns>The created database command.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager)
        {
            return CreateCommand(manager, manager.CommandTimeout);
        }

        /// <summary>
        /// Creates a new database command with the specified command timeout.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <param name="commandTimeout">The command timeout in seconds.</param>
        /// <returns>The created database command.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager, int commandTimeout)
        {
            var cmd = manager.Connection.OpenIfNeeded().CreateCommand();

            if (commandTimeout != 0)
                cmd.CommandTimeout = commandTimeout;

            cmd.Transaction = manager.Transaction;
            return cmd;
        }

        internal static DbParameter AddParam(this DbCommand command, string name, object value)
        {
            if (value is QueryParam queryParam)
                return queryParam.Init(command);

            DbParameter param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            command.Parameters.Add(param);

            return param;
        }
    }
}
