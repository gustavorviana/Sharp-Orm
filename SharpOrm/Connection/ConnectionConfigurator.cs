using System;
using System.Data.Common;

namespace SharpOrm.Connection
{
    public abstract class ConnectionConfigurator<TConnection> : IConnectionConfigurator
    {
        public abstract void Configure(TConnection connection);

        public virtual void Configure(DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection is TConnection conn)
                Configure(conn);
            else
                throw new NotSupportedException(
                    $"Connection type '{connection?.GetType().Name ?? "null"}' is not supported. " +
                    $"Expected type: '{typeof(TConnection).Name}'."
                );
        }
    }
}