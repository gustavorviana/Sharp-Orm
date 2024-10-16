﻿using DbRunTest.BaseTests;
using DbRunTest.Fixtures;
using MySql.Data.MySqlClient;
using Xunit.Abstractions;

namespace DbRunTest.Mysql
{
    public class SqlServerTableTest(ITestOutputHelper output, UnsafeDbFixture<MySqlConnection> connection) : DbTableTest<MySqlConnection>(output, connection)
    {
    }
}
