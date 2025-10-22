using BaseTest.Fixtures;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using System.Data.SqlClient;

namespace DbRunTest.Collections
{
    // Mock Collections
    [CollectionDefinition("Mock")]
    public class MockCollection : ICollectionFixture<MockFixture<SqliteQueryConfig>>
    {
    }

    // MySQL Collections
    [CollectionDefinition("MySQL", DisableParallelization = true)]
    public class MySqlCollection : ICollectionFixture<DbFixture<MySqlConnection>>
    {
    }

    [CollectionDefinition("MySQL Unsafe", DisableParallelization = true)]
    public class UnsafeMySqlCollection : ICollectionFixture<UnsafeDbFixture<MySqlConnection>>
    {
    }

    // SQLite Collections
    [CollectionDefinition("SQLite", DisableParallelization = true)]
    public class SqliteCollection : ICollectionFixture<DbFixture<SqliteConnection>>
    {
    }

    [CollectionDefinition("SQLite Unsafe", DisableParallelization = true)]
    public class UnsafeSqliteCollection : ICollectionFixture<UnsafeDbFixture<SqliteConnection>>
    {
    }

    // SQL Server Collections
    [CollectionDefinition("SQL Server", DisableParallelization = true)]
    public class SqlServerCollection : ICollectionFixture<DbFixture<SqlConnection>>
    {
    }

    [CollectionDefinition("SQL Server Unsafe", DisableParallelization = true)]
    public class UnsafeSqlServerCollection : ICollectionFixture<UnsafeDbFixture<SqlConnection>>
    {
    }
}
