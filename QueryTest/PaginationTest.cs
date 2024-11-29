using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using Bogus;
using SharpOrm;

namespace QueryTest
{
    public class PaginationTest : DbMockFallbackTest
    {
        [Fact]
        public virtual void Paginate()
        {
            this.ConfigureCount(10, "SELECT COUNT(*) FROM [TestTable]");
            this.ConfigureTestTable(8, "SELECT * FROM [TestTable] ORDER BY [Id] ASC OFFSET 0 ROWS FETCH NEXT 8 ROWS ONLY");

            using var query = new Query(TestTableUtils.TABLE).OrderBy("Id");
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
            this.ConfigureCount(4, "SELECT COUNT(DISTINCT [name]) FROM [TestTable]");
            this.ConfigureTestTable(4, "SELECT DISTINCT [name] FROM [TestTable] ORDER BY [name] ASC OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY");

            using var fallback = this.RegisterFallback();
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy(TestTableUtils.NAME)
                .Select(TestTableUtils.NAME)
                .Distinct = true;

            var pager = query.PaginateRows(5, 1);

            Assert.NotNull(pager);
            Assert.Equal(4, pager.Count);
            Assert.Equal(4, pager.Total);
        }

        [Fact]
        public void PaginateOrderBy()
        {
            this.ConfigureCount(10, "SELECT COUNT(*) FROM [TestTable]");
            this.ConfigureTestTable(8, "SELECT * FROM [TestTable] ORDER BY [Id] ASC OFFSET 0 ROWS FETCH NEXT 8 ROWS ONLY");

            using var query = new Query(TestTableUtils.TABLE).OrderBy("Id");
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
            this.ConfigureCount(10, "SELECT COUNT(*) FROM [TestTable]");
            this.ConfigureTestTable(8, "SELECT * FROM [TestTable] ORDER BY [Id] ASC OFFSET 0 ROWS FETCH NEXT 8 ROWS ONLY");

            using var fallback = this.RegisterFallback();
            using var query = new Query<TestTable>().OrderBy("Id");
            using var pager = query.PaginateRows(8, 1);
            pager.GoToPage(2);

            Assert.Equal("SELECT * FROM [TestTable] ORDER BY [Id] ASC OFFSET 8 ROWS FETCH NEXT 8 ROWS ONLY", fallback.ToString());
        }

        [Fact]
        public void PaginateDistinctColumn()
        {
            var faker = new Faker();
            this.ConfigureCount(4, "SELECT COUNT(DISTINCT name) FROM [TestTable]");
            this.Connection.QueryReaders["SELECT DISTINCT [name] FROM [TestTable] ORDER BY [name] ASC OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY"] =
                () => new MockDataReader(x => new Row(new Cell("name", faker.Name.FullName())), 4);

            using var query = new Query<TestTable>();
            query.OrderBy(TestTableUtils.NAME);
            query.Select(TestTableUtils.NAME);
            query.Distinct = true;

            var pager = query.Paginate(5, 1, (Column)"COUNT(DISTINCT name)");

            Assert.NotNull(pager);
            Assert.Equal(4, pager.Count);
            Assert.Equal(4, pager.Total);
        }

        [Fact]
        public virtual void PaginateWithForeign()
        {
            var fakerOrder = new Faker<Order>()
                        .RuleFor(x => x.Id, f => f.IndexFaker + 1)
                        .RuleFor(x => x.CustomerId, f => (uint)f.Random.Int(1, 10))
                        .RuleFor(x => x.Product, f => f.Commerce.ProductName())
                        .RuleFor(x => x.Quantity, f => f.Random.Int(0, 100))
                        .RuleFor(x => x.Status, f => f.PickRandom("Ok", "Pending"));

            var order = fakerOrder.Generate();

            var fakeCustomer = new Faker<Customer>()
                .RuleFor(x => x.Id, f => order.CustomerId)
                .RuleFor(x => x.Name, f => f.Person.FullName)
                .RuleFor(x => x.Email, f => f.Internet.Email());

            var customer = fakeCustomer.Generate();

            this.ConfigureCount(1, "SELECT COUNT(*) FROM [Orders] WHERE [customer_id] = 2");
            this.Connection.QueryReaders["SELECT * FROM [Orders] WHERE [customer_id] = 2 ORDER BY [Id] ASC OFFSET 0 ROWS FETCH NEXT 2 ROWS ONLY"] =
                () => new MockDataReader(i => Row.Parse(order), 1);
            this.Connection.QueryReaders["SELECT TOP(1) * FROM [Customers] WHERE [Id] = " + customer.Id] = () => new MockDataReader(i => Row.Parse(customer), 1);

            using var fallback = this.RegisterFallback();
            using var query = new Query<Order>();
            query.AddForeign(o => o.Customer).OrderBy("Id").Where("customer_id", 2);

            using var pager = query.Paginate(2, 1);

            Assert.NotNull(pager[0].Customer);
            Assert.Equivalent(pager[0].Customer, customer);
            Assert.Equal(pager[0].CustomerId, order.CustomerId);
            Assert.Equal(pager[0].Customer.Id, customer.Id);
        }

        private void ConfigureCount(int value, string query)
        {
            this.Connection.QueryReaders[query] = () => new MockDataReader(new Cell("COUNT(*)", value));
        }

        private void ConfigureTestTable(int items, string query)
        {
            this.Connection.QueryReaders[query] = () => MockDataReader.FromFaker(TestTableUtils.Faker(), items);
        }
    }
}
