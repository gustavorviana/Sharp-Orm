using BaseTest.Fixtures;
using BaseTest.Models;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.SqlMethods;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryLampdaTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        [Fact]
        public void SelectColumnWithoutJoin_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Select(c => c.Customer.Name);

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property, there are no joins for the 'Customers' table or defined for the 'BaseTest.Models.Customer' type.", exception.me);
        }
    }
}
