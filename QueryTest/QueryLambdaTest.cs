using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.SqlMethods;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryLambdaTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbMockFallbackTest(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        [Fact]
        public void SelectColumnWithoutJoin_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Select(c => c.Customer.Name);

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property, there are no joins for the 'Customers' table or defined for the 'BaseTest.Models.Customer' type.", exception.Message);
        }

        [Fact]
        public void JoinWithLambda()
        {
            using var fallback = RegisterFallback();

            using var query = new Query<Order>();
            query.Join(x => x.Customer, x => x.Id, x => x.CustomerId);
            query.Get();

            Assert.Equal("SELECT * FROM [Orders] INNER JOIN [Customers] ON [Customers].[Id] = [Orders].[customer_id]", fallback.ToString());
        }
    }
}
