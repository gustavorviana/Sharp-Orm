using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class EscapeStringTest(ITestOutputHelper output) : DbGrammarTestBase(output, new SqlServerQueryConfig { EscapeStrings = true })
    {
        [Fact]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;
            using var query = NewQuery(TestTableUtils.TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Name] = 'Mike' AND [Date] = @p1 AND [Alias] = '\"Mik\";''Mik''#--'", sqlExpression);
        }
    }
}
