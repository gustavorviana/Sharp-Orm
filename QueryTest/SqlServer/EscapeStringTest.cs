using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class EscapeStringTest(ITestOutputHelper output) : DbGrammarTestBase(output, new SqlServerQueryConfig { EscapeStrings = true }), IEscapeStringSelect
    {
        [Fact]
        public void Select()
        {
            var today = DateTime.Today;
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Name] = 'Mike' AND [Date] = ? AND [Alias] = '\"Mik\";''Mik''#--'", sqlExpression);
        }

        [Fact]
        public void SelectWhereSqlExpression()
        {
            using var query = new Query<TestTable>();
            const string Name = "Test";

            query.Where(new SqlExpression("Name COLLATE Latin1_General_CI_AI LIKE ?", $"%{Name}%"));
            QueryAssert.EqualDecoded($"SELECT * FROM [TestTable] WHERE Name COLLATE Latin1_General_CI_AI LIKE '%{Name}%'", [], query.Grammar().Select());
        }
    }
}
