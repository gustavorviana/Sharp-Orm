using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Connection
{
    public static class ConnectionUtils
    {
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

        internal static object ExecuteScalar(this ConnectionManager manager, SqlExpression expression, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using (var cmd = manager.GetCommand(expression).SetCancellationToken(token))
                {
                    Grammar.QueryLogger?.Invoke(cmd.CommandText);
                    return cmd.ExecuteScalar();
                }
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        internal static int ExecuteAndGetAffected(this ConnectionManager manager, SqlExpression expression, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using (var cmd = manager.GetCommand(expression).SetCancellationToken(token))
                {
                    Grammar.QueryLogger?.Invoke(cmd.CommandText);
                    using (var reader = cmd.ExecuteReader())
                        return reader.RecordsAffected;
                }
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        internal static int ExecuteNonQuery(this ConnectionManager manager, SqlExpression expression, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using (var cmd = manager.GetCommand(expression).SetCancellationToken(token))
                {
                    Grammar.QueryLogger?.Invoke(cmd.CommandText);
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        public static DbCommand GetCommand(this ConnectionManager manager, SqlExpression expression, CancellationToken token)
        {
            return manager.GetCommand(expression).SetCancellationToken(token);
        }

        public static DbCommand GetCommand(this ConnectionManager manager, SqlExpression expression)
        {
            return manager.GetCommand().SetExpression(expression);
        }

        public static DbCommand GetCommand(this ConnectionManager manager)
        {
            var cmd = manager.Connection.OpenIfNeeded().CreateCommand();
            cmd.CommandTimeout = manager.CommandTimeout;
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
