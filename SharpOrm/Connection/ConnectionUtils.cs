using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.DataTranslation;
using SharpOrm.SqlMethods;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Connection
{
    /// <summary>
    /// Provides utility methods for database connection management and SQL execution operations.
    /// </summary>
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
        /// Gets the server version from the database connection.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>The server version as a <see cref="Version"/> object.</returns>
        internal static Version GetVersion(this DbConnection connection)
        {
            return StringUtils.ParseVersionString(connection.ServerVersion);
        }

        /// <summary>
        /// Opens the database connection asynchronously if it is not already open.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>A task representing the asynchronous operation, with the opened database connection.</returns>
        /// <exception cref="Errors.DbConnectionException">Thrown when there is an error opening the connection.</exception>
        public static async Task<DbConnection> OpenIfNeededAsync(this DbConnection connection)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync();

                return connection;
            }
            catch (Exception ex)
            {
                throw new Errors.DbConnectionException(ex);
            }
        }

        /// <summary>
        /// Opens the database connection asynchronously with a cancellation token if it is not already open.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the opened database connection.</returns>
        /// <exception cref="Errors.DbConnectionException">Thrown when there is an error opening the connection.</exception>
        public static async Task<DbConnection> OpenIfNeededAsync(this DbConnection connection, CancellationToken token)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    await connection.OpenAsync(token);

                return connection;
            }
            catch (Exception ex)
            {
                throw new Errors.DbConnectionException(ex);
            }
        }

        /// <summary>  
        /// Executes a SQL statement against a connection object asynchronously and returns the result as an array of type <typeparamref name="T"/>.  
        /// </summary>  
        /// <typeparam name="T">The type of the elements in the returned array.</typeparam>  
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>  
        /// <param name="sql">The SQL query to be executed.</param>  
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>  
        /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>  
        /// <returns>A task representing the asynchronous operation, with an array of type <typeparamref name="T"/> representing the query results.</returns>  
        public static Task<T[]> ExecuteArrayAsync<T>(this ConnectionManager manager, string sql, TranslationRegistry registry, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteArray<T>(manager, sql, registry, token));
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
        public static T[] ExecuteArray<T>(this ConnectionManager manager, string sql, TranslationRegistry registry, CancellationToken token = default)
        {
            return ExecuteArray<T>(manager, new SqlExpression(sql), registry, token);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object asynchronously and returns the result as an array of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned array.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with an array of type <typeparamref name="T"/> representing the query results.</returns>
        public static Task<T[]> ExecuteArrayAsync<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteArray<T>(manager, expression, registry, token));
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
        public static T[] ExecuteArray<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
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
        public static IEnumerable<T> ExecuteEnumerable<T>(this ConnectionManager manager, string sql, TranslationRegistry registry, CancellationToken token = default)
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
        public static IEnumerable<T> ExecuteEnumerable<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
        {
            return new DbCommandEnumerable<T>(manager.CreateCommand(expression).SetCancellationToken(token), registry, manager.Management, token)
            {
                manager = manager
            };
        }

        /// <summary>
        /// Executes a SQL statement against a connection object asynchronously and returns the number of rows affected.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected by the SQL query.</returns>
        public static Task<int> ExecuteNonQueryAsync(this ConnectionManager manager, string sql, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteNonQuery(manager, sql, token));
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
        /// Executes a SQL statement against a connection object asynchronously and returns the number of rows affected.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the number of rows affected by the SQL query.</returns>
        public static Task<int> ExecuteNonQueryAsync(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteNonQuery(manager, expression, token));
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
            using (var cmd = manager.GetCommand().AddCancellationToken(token))
            {
                cmd.SetExpression(expression);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        public static Task<T> ExecuteScalarAsync<T>(this ConnectionManager manager, string sql, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar<T>(manager, sql, token));
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalar&lt;T&gt;(ExecuteScalarAsync&lt;T&gt;(ConnectionManager, string, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalar<T>(ExecuteScalarAsync<T>(ConnectionManager, string, CancellationToken)", false)]
        public static Task<T> ExecuteScalarAsync<T>(this ConnectionManager manager, string sql, TranslationRegistry registry, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar<T>(manager, sql, registry, token));
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
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="sql">The SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalar&lt;T&gt;(ConnectionManager, string, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalar<T>(ConnectionManager, string, CancellationToken)", false)]
        public static T ExecuteScalar<T>(this ConnectionManager manager, string sql, TranslationRegistry registry, CancellationToken token = default)
        {
            return ExecuteScalar<T>(manager, new SqlExpression(sql), registry, token);
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        public static Task<T> ExecuteScalarAsync<T>(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar<T>(manager, expression, token));
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalarAsync&lt;T&gt;(ConnectionManager, SqlExpression, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalarAsync<T>(ConnectionManager, SqlExpression, CancellationToken)", false)]
        public static Task<T> ExecuteScalarAsync<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar<T>(manager, expression, registry, token));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        public static T ExecuteScalar<T>(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            using (var cmd = manager.GetCommand().AddCancellationToken(token))
            {
                cmd.SetExpression(expression);
                return cmd.ExecuteScalar<T>();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">The type to which the returned value should be converted.</typeparam>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set, converted to type <typeparamref name="T"/>.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalar(ConnectionManager, SqlExpression, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalarAsync(ConnectionManager, SqlExpression, CancellationToken)", false)]
        public static T ExecuteScalar<T>(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
        {
            using (var cmd = manager.GetCommand().AddCancellationToken(token))
            {
                cmd.SetExpression(expression);
                return cmd.ExecuteScalar<T>();
            }
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set.</returns>
        public static Task<object> ExecuteScalarAsync(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar(manager, expression, token));
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with the value of the first column of the first row in the result set.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalarAsync(ConnectionManager, SqlExpression, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalarAsync(ConnectionManager, SqlExpression, CancellationToken)", false)]
        public static Task<object> ExecuteScalarAsync(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
        {
            return TaskUtils.Async(() => ExecuteScalar(manager, expression, registry, token));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        /// <remarks>This method will be removed in version 4.0. Use ExecuteScalar(ConnectionManager, SqlExpression, CancellationToken) instead.</remarks>
        [Obsolete("This method will be removed in version 4.0. Use ExecuteScalar(ConnectionManager, SqlExpression, CancellationToken)", false)]
        public static object ExecuteScalar(this ConnectionManager manager, SqlExpression expression, TranslationRegistry registry, CancellationToken token = default)
            => ExecuteScalar(manager, expression, token);

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> representing the SQL query to be executed.</param>
        /// <param name="token">Optional. A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>The value of the first column of the first row in the result set.</returns>
        public static object ExecuteScalar(this ConnectionManager manager, SqlExpression expression, CancellationToken token = default)
        {
            using (var cmd = new CommandBuilder(manager).AddCancellationToken(token))
            {
                cmd.SetExpression(expression);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Creates a new database command with the specified SQL expression and default command timeout.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="expression">The <see cref="SqlExpression"/> to be executed.</param>
        /// <returns>The created <see cref="DbCommand"/> configured with the SQL expression.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager, SqlExpression expression)
        {
            return CreateCommand(manager, manager.CommandTimeout).SetExpression(manager.Config, expression);
        }

        /// <summary>
        /// Creates a new database command with the default command timeout.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <returns>The created <see cref="DbCommand"/>.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager)
        {
            return CreateCommand(manager, manager.CommandTimeout);
        }

        /// <summary>
        /// Creates a new database command with the specified command timeout.
        /// </summary>
        /// <param name="manager">The <see cref="ConnectionManager"/> that manages the database connection.</param>
        /// <param name="commandTimeout">The command timeout in seconds. Use 0 for no timeout.</param>
        /// <returns>The created <see cref="DbCommand"/> configured with the specified timeout.</returns>
        public static DbCommand CreateCommand(this ConnectionManager manager, int commandTimeout)
        {
            var cmd = manager.Connection.OpenIfNeeded().CreateCommand();

            if (commandTimeout != 0)
                cmd.CommandTimeout = commandTimeout;

            cmd.Transaction = manager.Transaction;
            return cmd;
        }

        /// <summary>
        /// Adds a parameter to the database command with the specified name and value.
        /// </summary>
        /// <param name="command">The database command to add the parameter to.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value. If null, <see cref="DBNull.Value"/> will be used.</param>
        /// <returns>The created <see cref="DbParameter"/>.</returns>
        /// <remarks>
        /// If the value is a <see cref="QueryParam"/>, it will be initialized using its own logic.
        /// Otherwise, a standard parameter will be created with the provided name and value.
        /// </remarks>
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