using SharpOrm.Builder;
using SharpOrm.Errors;
using System;
using System.Data.Common;

namespace SharpOrm
{
    /// <summary>
    /// Default configuration for Query.
    /// </summary>
    public class QueryDefaults
    {
        public static QueryDefaults Default { get; set; }

        protected QueryDefaults()
        {

        }

        public QueryDefaults(IQueryConfig config, DbConnection connection)
        {
            this.Config = config;
            this.Connection = connection;
        }

        /// <summary>
        /// IQueryConfig defaults to "Query". The default object is "DefaultQueryConfig"
        /// </summary>
        public virtual IQueryConfig Config { get; }

        /// <summary>
        /// Default connection to a Query object. The default connection is null.
        /// </summary>
        public virtual DbConnection Connection { get; }

        public virtual DbTransaction Transaction { get; protected set; }

        public static void ExecuteTransaction(Action action)
        {
            if (Default.Connection == null)
                throw new ArgumentNullException(nameof(Default.Connection), "A default connection has not been set.");

            if (Default.Transaction != null)
                throw new DatabaseException("A transaction has already been started.");

            Default.Transaction = Default.Connection.BeginTransaction();

            try
            {
                action();
                Default.Transaction.Commit();
            }
            catch
            {
                Default.Transaction.Rollback();
                throw;
            }
            finally
            {
                Default.Transaction.Dispose();
                Default.Transaction = null;
            }
        }

        public static T ExecuteTransaction<T>(Func<T> func)
        {
            T value = default;
            ExecuteTransaction(() => value = func());
            return value;
        }
    }
}