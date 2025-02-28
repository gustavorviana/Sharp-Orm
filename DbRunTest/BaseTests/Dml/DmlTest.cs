using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class DmlTest<T> : DbTestBase, IClassFixture<DbFixture<T>> where T : DbConnection, new()
    {
        private UnsafeDbFixture<T>? unsafeFixture;

        private readonly HashSet<string> tablesToReset = [];


        public DmlTest(ITestOutputHelper output, DbFixture<T> connection) : base(output, connection)
        {
        }

        public void ConfigureInitialCustomerAndOrder()
        {
            var fixture = new UnsafeDbFixture<T>();

            using var qOrder = new Query<Order>(fixture.Manager);
            using var qAddress = new Query<Address>(fixture.Manager);
            using var qCustomer = new Query<Customer>(fixture.Manager);
            qOrder.Delete();
            qCustomer.Delete();
            qAddress.Delete();

            qAddress.BulkInsert(
                new Address(1)
                {
                    Name = "Test 1",
                    Street = "Street 1"
                },
                new Address(2)
                {
                    Name = "Test 2",
                    Street = "Street 2"
                }
            );

            qCustomer.BulkInsert(
                new Customer
                {
                    Id = 1,
                    Name = "Ronaldo",
                    AddressId = 1,
                    Email = "ronaldo@email.com"
                },
                new Customer
                {
                    Id = 2,
                    Name = "Michael",
                    AddressId = 2,
                    Email = "michael@email.com"
                }
            );

            qOrder.BulkInsert(
                new Order
                {
                    Id = 1,
                    CustomerId = 1,
                    Product = "My product",
                    Quantity = 10,
                    Status = "Pending"
                },
                new Order
                {
                    Id = 2,
                    CustomerId = 2,
                    Product = "My product 2",
                    Quantity = 10,
                    Status = "Ok"
                },
                new Order
                {
                    Id = 3,
                    CustomerId = 2,
                    Product = "My product 3",
                    Quantity = 10,
                    Status = "Ok"
                }
            );
        }

        protected void InsertRows(int count)
        {
            using var q = NewQuery(GetUnsafeManager(), TestTableUtils.TABLE);
            q.Delete();
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = TestTableUtils.NewRow(i, $"User {i}", i);

            q.BulkInsert(rows);
        }

        protected void ClearTables(string[] tables)
        {
            foreach (var table in tables)
            {
                try
                {
                    using var query = new Query(table, GetUnsafeManager());
                    query.Delete();
                }
                catch
                {
                }
            }
        }

        protected override void OnUseTable(string name)
        {
            tablesToReset.Add(name);
        }

        protected ConnectionManager GetUnsafeManager()
        {
            unsafeFixture ??= new UnsafeDbFixture<T>();
            return unsafeFixture.Manager;
        }

        protected QueryConfig GetUnsafeConfig()
        {
            unsafeFixture ??= new UnsafeDbFixture<T>();
            return unsafeFixture.Manager.Config;
        }

        protected override void Dispose(bool disposing)
        {
            ClearTables([.. tablesToReset]);
            tablesToReset.Clear();

            unsafeFixture?.Dispose();
            unsafeFixture = null!;

            base.Dispose(disposing);
        }
    }
}
