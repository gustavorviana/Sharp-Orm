﻿using BaseTest.Utils;
using QueryTest.Fixtures;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class EscapeStringTest(ITestOutputHelper output) : DbGrammarTestBase(output, new SqliteQueryConfig { EscapeStrings = true }), IClassFixture<DbFixture<SqliteQueryConfig>>
    {
        [Fact]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;

            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"Name\" = 'Mike' AND \"Date\" = '2024-10-13T00:00:00' AND \"Alias\" = '\"Mik\";\''Mik\''#--'", query.Grammar().Select());
        }
    }
}