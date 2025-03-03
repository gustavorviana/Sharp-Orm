﻿using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerSelectTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : SelectTest<SqlConnection>(output, connection)
    {
    }
}
