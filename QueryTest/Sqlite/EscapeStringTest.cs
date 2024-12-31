using BaseTest.Fixtures;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class EscapeStringTest(ITestOutputHelper output) : DbGrammarTestBase(output, new SqliteQueryConfig { EscapeStrings = true }), IClassFixture<MockFixture<SqliteQueryConfig>>
    {
        [Fact]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;

            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            QueryAssert.Equal($"SELECT * FROM \"TestTable\" WHERE \"Name\" = 'Mike' AND \"Date\" = '{today.ToString(DateTranslation.Format)}' AND \"Alias\" = '\"Mik\";\''Mik\''#--'", query.Grammar().Select());
        }
    }
}
