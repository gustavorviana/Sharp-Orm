using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using BaseTest.Fixtures;
using Xunit.Abstractions;
using SharpOrm;

namespace BaseTest.Utils
{
    public abstract class DbTestBase : QueryTestBase
    {
        #region Fields/Properties
        private readonly HashSet<string> tablesToReset = [];

        protected ConnectionManager Manager => connection.Manager;
        public ConnectionCreator Creator => connection.Creator;
        public QueryConfig Config => connection.Creator.Config;

        protected virtual bool ResetTablesOnEnd { get; set; }
        #endregion

        public DbTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output, connection)
        {
        }

        protected Query NewQuery(string table, string alias = "", QueryConfig? config = null)
        {
            tablesToReset.Add(table);
            return new Query(new DbName(table, alias), GetManager(config));
        }

        protected Query<T> NewQuery<T>(string alias = "", QueryConfig? config = null)
        {
            tablesToReset.Add(TranslationRegistry.Default.GetTable(typeof(T)).Name);
            return new Query<T>(alias, GetManager(config));
        }

        protected ConnectionManager GetManager(QueryConfig? config = null)
        {
            return config == null ? Manager : NewConnectionManager(config);
        }

        protected ConnectionManager NewConnectionManager(QueryConfig? config = null)
        {
            return new ConnectionManager(config ?? Config, Creator.GetConnection()) { Management = ConnectionManagement.CloseOnManagerDispose };
        }

        protected void ClearTables(params string[] tables)
        {
            foreach (var table in tables)
            {
                try
                {
                    using var query = new Query(table, Manager);
                    query.Delete();
                }
                catch
                {
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (ResetTablesOnEnd)
                ClearTables([.. tablesToReset]);

            base.Dispose(disposing);
        }
    }
}
