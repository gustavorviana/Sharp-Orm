using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class UpdateTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DmlTest<T>(output, connection), IClassFixture<UnsafeDbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public void Update()
        {
            const int Id = 1;
            using var query = NewQuery(TestTableUtils.TABLE);
            query.Insert(TestTableUtils.NewRow(Id, "Name1").Cells);

            query.Where(TestTableUtils.ID, Id);
            query.Update(new Cell(TestTableUtils.NAME, "Name2"));

            var row = query.FirstRow();
            Assert.NotNull(row);
            Assert.Equal("Name2", row[TestTableUtils.NAME]);
        }

        [Fact]
        public void UpdateObject()
        {
            const int Id = 1;
            using var query = NewQuery<TestTable>();
            query.Insert(new TestTable
            {
                Id = Id,
                Name = "Name1"
            });

            query.Where(TestTableUtils.ID, Id);
            query.Update(new TestTable
            {
                Id = Id,
                Name = "Name2"
            });

            query.Update(new TestTable
            {
                Nick = "Tester"
            }, TestTableUtils.NICK);

            var row = query.FirstRow();
            Assert.NotNull(row);
            Assert.Equal("Name2", row[TestTableUtils.NAME]);
            Assert.Equal("Tester", row[TestTableUtils.NICK]);
            Assert.Throws<InvalidOperationException>(() => query.Update(new TestTable(), "Invalid"));
        }

        [Fact]
        public void UpdateWhereIsNull()
        {
            const int Id = 1;
            using var query = NewQuery(TestTableUtils.TABLE);
            query.Insert(TestTableUtils.NewRow(Id, "Name1").Cells);

            query.Where(TestTableUtils.NICK, null);
            bool changed = query.Update(new Cell(TestTableUtils.NICK, "Name2")) > 0;
            Assert.True(changed);

            using var query2 = NewQuery(TestTableUtils.TABLE);
            query2.Where(TestTableUtils.ID, Id);

            var row = query2.FirstRow();
            Assert.NotNull(row);
            Assert.Equal("Name2", row[TestTableUtils.NICK]);
        }

        [Fact]
        public virtual void UpdateJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Creator);
            using var qCustomer = new Query<Customer>(Creator);

            qOrder.Join<Customer>("c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Update(new Cell("Status", "Processed"));

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            qOrder.Offset = 1;
            var order = qOrder.FirstOrDefault();
            Assert.Equal("Ok", order.Status);

            qOrder.Offset = null;
            order = qOrder.FirstOrDefault();
            Assert.Equal("Processed", order.Status);
        }
    }
}
