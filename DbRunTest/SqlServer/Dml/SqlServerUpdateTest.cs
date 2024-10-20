using BaseTest.Models;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer.Dml
{
    public class SqlServerUpdateTest(ITestOutputHelper output, DbFixture<SqlConnection> connection) : UpdateTest<SqlConnection>(output, connection)
    {
        [Fact]
        public override void UpdateJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Creator);
            using var qCustomer = new Query<Customer>(Creator);

            qOrder.Join<Customer>("c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Update(new Cell("Status", "Processed"));

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            qOrder.OrderBy("Id");
            qOrder.Offset = 1;
            var order = qOrder.FirstOrDefault();
            Assert.Equal("Ok", order.Status);

            qOrder.Offset = null;
            order = qOrder.FirstOrDefault();
            Assert.Equal("Processed", order.Status);
        }
    }
}
