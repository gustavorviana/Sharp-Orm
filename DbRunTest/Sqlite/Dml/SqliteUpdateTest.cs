using BaseTest.Models;
using DbRunTest.BaseTests.Dml;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using SharpOrm;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite.Dml
{
    public class SqliteUpdateTest(ITestOutputHelper output, DbFixture<SqliteConnection> connection) : UpdateTest<SqliteConnection>(output, connection)
    {
        public override void UpdateJoin()
        {
            using var qOrder = new Query<Order>(Creator);
            using var qCustomer = new Query<Customer>(Creator);

            qOrder.Join("Customers c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            Assert.Throws<NotSupportedException>(() => qOrder.Update(new Cell("Status", "Processed")));
        }
    }
}
