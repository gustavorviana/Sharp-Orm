using BaseTest.Fixtures;
using DbRunTest.Fixtures;

namespace DbRunTest.Collections
{
    [CollectionDefinition("Mock")]
    public class MockCollection : ICollectionFixture<MockFixture>
    {
    }

    [CollectionDefinition("MySQL", DisableParallelization = true)]
    public class MySqlCollection : ICollectionFixture<MysqlFixture>
    {
    }

    [CollectionDefinition("SQLite", DisableParallelization = true)]
    public class SqliteCollection : ICollectionFixture<SqliteFixture>
    {
    }

    [CollectionDefinition("SQL Server", DisableParallelization = true)]
    public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
    {
    }
}
