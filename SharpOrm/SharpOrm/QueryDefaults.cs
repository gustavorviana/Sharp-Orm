using SharpOrm.Builder;
using SharpOrm.Errors;
using System;
using System.Data.Common;

namespace SharpOrm
{
    /// <summary>
    /// Default configuration for Query.
    /// </summary>
    public static class QueryDefaults
    {
        /// <summary>
        /// IQueryConfig defaults to "Query". The default object is "DefaultQueryConfig"
        /// </summary>
        public static IQueryConfig Config { get; set; } = new MysqlQueryConfig();

        /// <summary>
        /// Default connection to a Query object. The default connection is null.
        /// </summary>
        public static DbConnection Connection { get; set; }

        public static DbTransaction Transaction { get; private set; }

        public static void ExecuteTransaction(Action action)
        {
            if (Transaction != null)
                throw new DatabaseException("A transaction has already been started.");

            Transaction = Connection.BeginTransaction();

            try
            {
                action();
                Transaction.Commit();
            }
            catch
            {
                Transaction.Rollback();
                throw;
            }
            finally
            {
                Transaction.Dispose();
                Transaction = null;
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