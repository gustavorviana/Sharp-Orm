using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.Common;

namespace UnityTest.Utils
{
    public abstract class DbTest<Conn> : BaseTest where Conn : DbConnection, new()
    {
        protected SingleConnectionCreator<Conn> Creator { get; }
        protected QueryConfig Config => Creator.Config;

        public DbTest(QueryConfig config, string connStr)
        {
            this.Creator = new SingleConnectionCreator<Conn>(config, connStr);
        }

        protected static void ExecuteScript(string sql, DbConnection connection)
        {
            using var conn = connection.OpenIfNeeded();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        protected Query NewQuery(string table, string alias = "", QueryConfig config = null)
        {
            return new Query(new DbName(table, alias), GetConnectionManager(config));
        }

        protected ConnectionManager GetConnectionManager(QueryConfig config = null)
        {
            return new ConnectionManager(config ?? this.Config, this.Creator.GetConnection()) { Management = ConnectionManagement.CloseOnManagerDispose }; ;
        }

        [TestCleanup]
        public void CleanupTest()
        {
            ConnectionCreator.Default = null;
        }

        protected void ClearTable(string table)
        {
            using var query = new Query(table);
            query.Delete();
        }
    }
}
