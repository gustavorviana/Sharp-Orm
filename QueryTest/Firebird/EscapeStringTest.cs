using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Fb;
using Xunit.Abstractions;

namespace QueryTest.Firebird
{
    public class EscapeStringTest(ITestOutputHelper output) : DbGrammarTestBase(output, new FbQueryConfig { EscapeStrings = true })
    {
        [Fact]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;

            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"Name\" = 'Mike' AND \"Date\" = ? AND \"Alias\" = '\"Mik\";''Mik''#--'", query.Grammar().Select());
        }
    }
}