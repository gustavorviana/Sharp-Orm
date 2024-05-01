using System;
using System.Data.Common;

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

        public static DbCommand GetCommand(this ConnectionManager manager)
        {
            var cmd = manager.Connection.OpenIfNeeded().CreateCommand();
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
