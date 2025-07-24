using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerPaginationTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : PaginationTest<SqlConnection>(output, connection)
    {
        [Fact]
        public override void GotoPage()
        {
            InsertRows(10);

            using var query = NewQuery(TestTableUtils.TABLE);
            query.OrderBy("Id");
            using var pager = query.PaginateRows(8, 1);
            pager.GoToPage(2);

            Assert.NotNull(pager);
            Assert.Equal(2, pager.Count);
            Assert.Equal(2, pager.CurrentPage);
            Assert.Equal(10, pager.Total);
        }

        [Fact]
        public override void Paginate()
        {
            InsertRows(10);

            using var query = NewQuery(TestTableUtils.TABLE);
            query.OrderBy("Id");
            using var pager = query.PaginateRows(8, 1);

            Assert.NotNull(pager);
            Assert.Equal(8, pager.Count);
            Assert.Equal(2, pager.Pages);
            Assert.Equal(1, pager.CurrentPage);
            Assert.Equal(10, pager.Total);
        }

        [Fact]
        public override void PaginateWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = NewQuery<Order>();
            query.OrderBy("Id");
            var orders = ((Query<Order>)query.Where("customer_id", 2)).AddForeign(o => o.Customer.Address).Paginate(2, 1);

            Assert.NotNull(orders[0].Customer);
            Assert.Equivalent(orders[0].Customer, orders[1].Customer);
            Assert.Equal(orders[0].CustomerId, orders[1].CustomerId);
            Assert.Equal(orders[0].Customer.Id, orders[1].Customer.Id);

            Assert.NotNull(orders[0].Customer.Address);
        }

        [Fact]
        public void PaginateWithoutOrderby()
        {
            using var q = new Query<TestTable>(Creator);
            Assert.Throws<InvalidOperationException>(() => q.Paginate(1, 2));
        }
    }
}
