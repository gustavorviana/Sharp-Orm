using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using SharpOrm.Builder.Grammars;
using System.Data.Common;
using System.Diagnostics;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class PaginationTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DmlTest<T>(output, connection), IClassFixture<UnsafeDbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public virtual void Paginate()
        {
            InsertRows(10);

            using var query = NewQuery(TestTableUtils.TABLE);
            using var pager = query.PaginateRows(8, 1);

            Assert.NotNull(pager);
            Assert.Equal(8, pager.Count);
            Assert.Equal(2, pager.Pages);
            Assert.Equal(1, pager.CurrentPage);
            Assert.Equal(10, pager.Total);
        }

        [Fact]
        public void PaginateDistinct()
        {
            InsertRows(4);

            var q = NewQuery<TestTable>();
            q.Insert(TestTableUtils.NewRow(6, "User 1").Cells);
            q.OrderBy(TestTableUtils.NAME);
            q.Distinct = true;
            q.Select(TestTableUtils.NAME);
            var r = q.Paginate(5, 1);

            Assert.NotNull(r);
            Assert.Equal(4, r.Count);
            Assert.Equal(4, r.Total);
        }

        [Fact]
        public void PaginateOrderBy()
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
        public virtual void GotoPage()
        {
            InsertRows(10);

            using var query = NewQuery(TestTableUtils.TABLE);
            using var pager = query.PaginateRows(8, 1);
            pager.GoToPage(2);

            Assert.NotNull(pager);
            Assert.Equal(2, pager.Count);
            Assert.Equal(2, pager.CurrentPage);
            Assert.Equal(10, pager.Total);
        }


        [Fact]
        public void PaginateDistinctColumn()
        {
            InsertRows(4);

            using var q = new Query<TestTable>(Creator);
            q.Insert(TestTableUtils.NewRow(6, "User 1").Cells);
            q.OrderBy(TestTableUtils.NAME);
            q.Distinct = true;
            q.Select(TestTableUtils.NAME);
            var r = q.Paginate(5, 1, (Column)"COUNT(DISTINCT name)");

            Assert.NotNull(r);
            Assert.Equal(4, r.Count);
            Assert.Equal(4, r.Total);
        }

        [Fact]
        public virtual void PaginateWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            Grammar.QueryLogger = (sql) => Debug.WriteLine(sql);
            using var query = NewQuery<Order>();
            var orders = query.Where(x => x.CustomerId, 2).AddForeign(o => o.Customer.Address).Paginate(2, 1);

            Assert.NotNull(orders[0].Customer);
            Assert.Equivalent(orders[0].Customer, orders[1].Customer);
            Assert.Equal(orders[0].CustomerId, orders[1].CustomerId);
            Assert.Equal(orders[0].Customer.Id, orders[1].Customer.Id);

            Assert.NotNull(orders[0].Customer.Address);
        }
    }
}
