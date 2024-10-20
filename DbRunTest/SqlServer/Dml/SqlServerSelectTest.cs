using BaseTest.Models;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerSelectTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : SelectTest<SqlConnection>(output, connection)
    {
        [Fact]
        public override void PaginateWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = NewQuery<Order>();
            query.OrderBy("Id");
            var orders = ((Query<Order>)query.Where("customer_id", 2)).AddForeign(o => o.Customer.Address).Paginate(2, 1);

            Assert.NotNull(orders[0].Customer);
            Assert.Equal(orders[0].Customer, orders[1].Customer);
            Assert.Equal(orders[0].CustomerId, orders[1].CustomerId);
            Assert.Equal(orders[0].Customer.Id, orders[1].Customer.Id);

            Assert.NotNull(orders[0].Customer.Address);
        }
    }
}
