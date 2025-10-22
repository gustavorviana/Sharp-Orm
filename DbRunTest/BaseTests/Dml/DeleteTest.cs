using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class DeleteTest(ITestOutputHelper output, DbFixtureBase connection) : DmlTest(output, connection)
    {
        [Fact]
        public void Delete()
        {
            using var query = NewQuery(GetUnsafeManager(), TestTableUtils.TABLE);
            query.Delete();
            query.BulkInsert(TestTableUtils.NewRow(1, "A"), TestTableUtils.NewRow(2, "B"));

            Assert.Equal(2, query.Count());

            query.Where(TestTableUtils.NAME, "B");
            query.Delete();

            using var query2 = NewQuery(TestTableUtils.TABLE);
            Assert.Equal(1, query2.Count());
        }

        [Fact]
        public virtual void DeleteWhereJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Creator);
            using var qCustomer = new Query<Customer>(Creator);

            qOrder.Join("Customers c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Delete();

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            Assert.Equal(2, qOrder.Count());

            var order = qOrder.FirstOrDefault();
            Assert.Equal("Ok", order.Status);
        }
    }
}
