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

        public static DbCommand SetCancellationToken(this DbCommand command, CancellationToken token)
        {
            CancellationTokenRegistration registry = default;
            token.Register(() =>
            {
                try { command.Cancel(); } catch { }
                registry.Dispose();
            });

            command.Disposed += (sender, e) => registry.Dispose();

            return command;
        }

        public static DbCommand GetCommand(this ConnectionManager manager, SqlExpression expression)
        {
            return manager.GetCommand().SetExpression(expression);
        }

        public static DbCommand GetCommand(this ConnectionManager manager)
        {
            return GetCommand(manager, manager.CommandTimeout);
        }

        public static DbCommand GetCommand(this ConnectionManager manager, int commandTimeout)
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
