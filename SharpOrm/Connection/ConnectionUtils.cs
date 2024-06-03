using SharpOrm.Builder;
using System;
using System.Data.Common;
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
        /// executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(this ConnectionManager manager, SqlExpression expression)
        {
            try
            {
                using (var cmd = manager.CreateCommand().SetExpression(expression))
                    return cmd.ExecuteNonQuery();
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public static T ExecuteScalar<T>(this ConnectionManager manager, SqlExpression expression)
        {
            try
            {
                using (var cmd = manager.CreateCommand().SetExpression(expression))
                    return cmd.ExecuteScalar<T>();
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Gets a command with the specified SQL expression.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <param name="expression">The SQL expression to set in the command.</param>
        /// <returns>The configured database command.</returns>
        public static DbCommand GetCommand(this ConnectionManager manager, SqlExpression expression)
        {
            return manager.CreateCommand().SetExpression(expression);
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
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);

            return param;
        }
    }
}
