using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class DmlTest(ITestOutputHelper output, DbFixtureBase connection) : DbTestBase(output, connection)
    {

        private readonly HashSet<string> tablesToReset = [];

        public void ConfigureInitialCustomerAndOrder()
        {
            using var manager = GetUnsafeManager();

            using var qOrder = new Query<Order>(manager);
            using var qAddress = new Query<Address>(manager);
            using var qCustomer = new Query<Customer>(manager);
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
            using var manager = GetUnsafeManager();
            using var q = NewQuery(manager, TestTableUtils.TABLE);
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
                    using var manager = GetUnsafeManager();
                    using var query = new Query(table, manager);
                    query.Delete();
                }
                catch
                {
                }
            }
        }

        protected ConnectionManager GetUnsafeManager()
        {
            return new ConnectionManager(GetUnsafeConfig(), Creator.GetConnection());
        }

        protected QueryConfig GetUnsafeConfig()
        {
            return Creator.Config.Clone(false);
        }

        protected override void OnUseTable(string name)
        {
            tablesToReset.Add(name);
        }

        public override async Task DisposeAsync()
        {
            ClearTables([.. tablesToReset]);
            tablesToReset.Clear();

            await base.DisposeAsync();
        }
    }
}
