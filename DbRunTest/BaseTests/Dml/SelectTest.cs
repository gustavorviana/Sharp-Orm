using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System.Data.Common;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class SelectTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DmlTest<T>(output, connection), IClassFixture<UnsafeDbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public void SelectDateTime()
        {
            using var query = NewQuery<TestTable>();

            var date = DateTime.Now.RemoveMiliseconds();
            query.Insert(new Cell("Id", 1), new Cell("Name", ""), new Cell("record_created", date), new Cell("number", 0), new Cell("custom_status", 0));
            DateTime dbDate = query.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.EqualDate(date, dbDate, "Time insert fail");
        }

        [Fact]
        public void SelectUtcDateTime()
        {
            var config = this.Config.Clone();
            config.Translation = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };

            using var query = NewQuery<TestTable>(this.GetManager(config));

            DateTime date = DateTime.Now.RemoveMiliseconds();
            query.Insert(new TestTable { Id = 1, Name = "", CreatedAt = date, Number = 0, CustomStatus = Status.Success });
            DateTime dbDate = query.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.EqualDate(date, dbDate, "Universal time read file");
        }

        [Fact]
        public void SelectDateTimeOffset()
        {
            using var query = NewQuery<TestTable>();

            var date = DateTimeOffset.Now.RemoveMiliseconds();
            query.Insert(new Cell("Id", 1), new Cell("Name", ""), new Cell("record_created", date), new Cell("number", 0), new Cell("custom_status", 0));
            DateTime dbDate = query.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.EqualDate(date.DateTime, dbDate, "Time insert fail");
        }

        [Fact]
        public void SelectUtcDateTimeOffset()
        {
            var config = this.Config.Clone();
            config.Translation = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };

            using var query = NewQuery<TestTable>(this.GetManager(config));

            var date = DateTimeOffset.Now.RemoveMiliseconds();
            query.Insert(new Cell("Id", 1), new Cell("Name", ""), new Cell("record_created", date), new Cell("number", 0), new Cell("custom_status", 0));
            DateTime dbDate = query.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.EqualDate(date.LocalDateTime, dbDate, "Universal time insert fail");
        }

        [Fact]
        public virtual void Select()
        {
            const int Id = 1;
            const string Name = "User 1";
            using var query = NewQuery(TestTableUtils.TABLE);

            query.Insert(TestTableUtils.NewRow(Id, Name).Cells);

            var row = query.FirstRow();

            Assert.NotNull(row);
            Assert.Equal(Id, Convert.ToInt32(row[TestTableUtils.ID]));
            Assert.Equal(Name, row[TestTableUtils.NAME]);
        }

        [Fact]
        public void SelectDistinct()
        {
            this.ClearTables([TestTableUtils.TABLE]);
            const int Id = 1;
            const string Name = "User 1";
            using var query = NewQuery(TestTableUtils.TABLE);

            query.Insert(TestTableUtils.NewRow(Id, Name).Cells);
            query.Distinct = true;

            var row = query.FirstRow();

            Assert.NotNull(row);
            Assert.StrictEqual(Id, Convert.ToInt32(row[TestTableUtils.ID]));
            Assert.Equal(Name, row[TestTableUtils.NAME]);
        }

        [Fact]
        public void SelectByValidPk()
        {
            const int Id = 1;
            const string Name = "User 1";
            using var query = NewQuery<TestTable>();

            query.Insert(new TestTable
            {
                Id = Id,
                Id2 = Id,
                Name = Name
            });

            var value = query.Find(Id, Id);
            Assert.NotNull(value);
        }

        [Fact]
        public void SelectGroupBy()
        {
            const string Name = "User 1";
            using var query = NewQuery<TestTable>(GetUnsafeManager());
            query.Delete();

            query.BulkInsert(
                new TestTable { Id = 1, Id2 = 1, Name = Name },
                new TestTable { Id = 2, Id2 = 1, Name = Name },
                new TestTable { Id = 3, Id2 = 2, Name = Name },
                new TestTable { Id = 4, Id2 = 2, Name = Name }
            );

            query.GroupBy("Id2");
            Assert.Equal(2, query.Count());
        }

        [Fact]
        public void SelectGroupByHaving()
        {
            const string Name = "User 1";
            using var query = NewQuery<TestTable>(GetUnsafeManager());
            query.Delete();

            query.BulkInsert(
                new TestTable { Id = 1, Id2 = 1, Name = Name },
                new TestTable { Id = 2, Id2 = 2, Name = Name },
                new TestTable { Id = 3, Id2 = 3, Name = Name },
                new TestTable { Id = 4, Id2 = 2, Name = Name }
            );

            query.GroupBy("Id2");
            query.Having(q => q.Where("Id2", "=", 1));
            Assert.Equal(1, query.Count());
        }

        [Fact]
        public void MultipleSelectCount()
        {
            InsertRows(4);

            using var q = NewQuery(TestTableUtils.TABLE);
            q.Select(TestTableUtils.ID, TestTableUtils.NAME, TestTableUtils.NICK);

            Assert.Equal(4, q.Count());
            Assert.Equal(4, q.Count("*"));
        }

        [Fact]
        public void SelectInvalidColumn()
        {
            var config = this.Config.Clone();
            config.LoadForeign = true;
            using var query = NewQuery<Customer>(GetManager(config));
            Assert.Throws<InvalidOperationException>(() => query.AddForeign(f => f.Address.Street));
        }

        [Fact]
        public void DeepSelect()
        {
            var config = this.GetUnsafeConfig();
            config.LoadForeign = true;
            const uint Id = 1;
            const int Addr = 1;
            const string Name = "User 1";
            const string Email = "my@email.com";
            using var addrQuery = NewQuery<Address>(GetUnsafeManager());
            using var query = NewQuery<Customer>(GetManager(config));

            query.Delete();
            addrQuery.Delete();

            addrQuery.Insert(new Address(1) { Name = "Addr", Street = "str" });
            query.Insert(new Cell("Id", Id), new Cell("Name", Name), new Cell("Email", Email), new Cell("address_id", Addr));

            var customer = query.AddForeign(f => f.Address).FirstOrDefault();

            Assert.NotNull(customer);
            Assert.NotNull(customer.Address);
            Assert.Equal(Id, customer.Id);
            Assert.Equal(Name, customer.Name);
            Assert.Equal(Email, customer.Email);
            Assert.Equal(customer.AddressId, customer.Address.Id);
            Assert.NotNull(customer.Address.Name);
        }

        [Fact]
        public void SelectWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = NewQuery<Order>();
            query.AddForeign(f => f.Customer).AddForeign(f => f.Customer.Address);
            var order = query.FirstOrDefault();

            Assert.NotNull(order.Customer);
            Assert.Equal(order.CustomerId, order.Customer.Id);

            Assert.NotNull(order.Customer.Address);
            Assert.Equal(order.Customer.AddressId, order.Customer.Address.Id);
        }

        [Fact]
        public void FindWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = NewQuery<Order>();
            var order = query.AddForeign(o => o.Customer.Address).Find(1);

            Assert.NotNull(order.Customer);
            Assert.Equal(order.CustomerId, order.Customer.Id);

            Assert.NotNull(order.Customer.Address);
            Assert.Equal(order.Customer.AddressId, order.Customer.Address.Id);
        }

        [Fact]
        public void ExecuteArrayScalar()
        {
            using var q = NewQuery(this.GetUnsafeManager(), TestTableUtils.TABLE);
            q.Delete();
            InsertRows(5);
            q.Select("Name");

            var names = q.ExecuteArrayScalar<string>();
            Assert.Equal(5, names.Length);
        }

        [Fact]
        public void ExecuteArrayScalarConvertion()
        {
            InsertRows(5);
            using var q = NewQuery(this.GetUnsafeManager(), TestTableUtils.TABLE);
            q.Select("Id");

            var names = q.ExecuteArrayScalar<string>();
            Assert.Equal(5, names.Length);
        }


        [Fact]
        public void SelectJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(this.Creator);

            qOrder.Join<Customer>("c", q => q.WhereColumn("c.id", "orders.customer_id").Where("c.Email", "!=", "Test"));
            qOrder.Where("c.name", "Ronaldo");
            Assert.NotNull(qOrder.FirstOrDefault());
        }

        [Fact]
        public void Paginate()
        {
            InsertRows(30);

            using var q = new Query<TestTable>("p", Creator);
            q.Select("p.*");
            q.OrderBy("Id");
            var r = q.Paginate(5, 1);

            Assert.NotNull(r);
            Assert.Equal(5, r.Count);
            Assert.Equal(30, r.Total);
            Assert.Equal(1, r.CurrentPage);
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
        public void PaginateDistinct()
        {
            InsertRows(4);

            using var q = new Query<TestTable>(Creator);
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
        public virtual void PaginateWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = NewQuery<Order>();
            var orders = ((Query<Order>)query.Where("customer_id", 2)).AddForeign(o => o.Customer.Address).Paginate(2, 1);

            Assert.NotNull(orders[0].Customer);
            Assert.Equal(orders[0].Customer, orders[1].Customer);
            Assert.Equal(orders[0].CustomerId, orders[1].CustomerId);
            Assert.Equal(orders[0].Customer.Id, orders[1].Customer.Id);

            Assert.NotNull(orders[0].Customer.Address);
        }
    }
}
