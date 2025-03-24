using BaseTest.Fixtures;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public abstract class DbTestBase : QueryTestBase
    {
        public DbTestBase(DbFixtureBase connection) : base(connection)
        {
        }

        public DbTestBase(ITestOutputHelper output, DbFixtureBase connection) : base(output, connection)
        {
        }

        protected Query NewQuery(string table, string alias = "", QueryConfig? config = null)
        {
            OnUseTable(table);
            return new Query(new DbName(table, alias), GetManager(config));
        }

        protected Query<T> NewQuery<T>(string alias = "", QueryConfig? config = null)
        {
            OnUseTable(TranslationRegistry.Default.GetTable(typeof(T)).Name);
            return new Query<T>(alias, GetManager(config));
        }


        protected Query NewQuery(ConnectionManager manager, string table, string alias = "")
        {
            OnUseTable(table);
            return new Query(new DbName(table, alias), manager);
        }

        protected Query<T> NewQuery<T>(ConnectionManager manager, string alias = "")
        {
            OnUseTable(TranslationRegistry.Default.GetTable(typeof(T)).Name);
            return new Query<T>(alias, manager);
        }

        protected virtual void OnUseTable(string name)
        {

        }
    }
}
