using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class MySqlCommandRunTest : MysqlTableTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery();
            q.Insert(NewRow(120, "User 1").Cells);
            int lastId = q.Insert(NewRow(null, "User 2").Cells);

            Assert.AreEqual(2, q.Count());
            Assert.AreEqual(121, lastId);
        }

        [TestMethod]
        public void Select()
        {
            const int Id = 1;
            const string Name = "User 1";
            using var query = NewQuery();

            query.Insert(NewRow(Id, Name).Cells);

            var row = query.FirstRow();

            Assert.IsNotNull(row);
            Assert.AreEqual(Id, row[ID]);
            Assert.AreEqual(Name, row[NAME]);
        }

        [TestMethod]
        public void MultipleSelectCount()
        {
            InsertRows(4);

            using var q = NewQuery();
            q.Select(ID, NAME, NICK);

            Assert.AreEqual(4, q.Count());
            Assert.AreEqual(4, q.Count("*"));
        }

        [TestMethod]
        public void SelectInvalidColumn()
        {
            var config = new MysqlQueryConfig(false) { LoadForeign = true };
            using var query = new Query<Customer>(GetConnectionManager(config));
            Assert.ThrowsException<InvalidOperationException>(() => query.AddForeign(f => f.Address.Street));
        }

        [TestMethod]
        public void DeepSelect()
        {
            var config = new MysqlQueryConfig(false) { LoadForeign = true };
            const uint Id = 1;
            const int Addr = 1;
            const string Name = "User 1";
            const string Email = "my@email.com";
            using var addrQuery = new Query<Address>(Creator);
            using var query = new Query<Customer>(GetConnectionManager(config));

            query.Delete();
            addrQuery.Delete();

            addrQuery.Insert(new Address(1) { Name = "Addr", Street = "str" });
            query.Insert(new Cell("Id", Id), new Cell("Name", Name), new Cell("Email", Email), new Cell("address_id", Addr));
            
            var customer = query.AddForeign(f => f.Address).FirstOrDefault();

            Assert.IsNotNull(customer, "Customer failed");
            Assert.IsNotNull(customer.Address, "Address failed");
            Assert.AreEqual(Id, customer.Id, "Customer Id failed");
            Assert.AreEqual(Name, customer.Name, "Customer Name failed");
            Assert.AreEqual(Email, customer.Email, "Customer Email failed");
            Assert.AreEqual(customer.AddressId, customer.Address.Id, "Address Id failed");
            Assert.IsNotNull(customer.Address.Name);
        }

        [TestMethod]
        public void ForeignObjectInsert()
        {
            const uint Id = 1;
            const string Name = "User 1";
            const string Email = "my@email.com";
            using var query = new Query<Customer>(Creator);

            query.Delete();

            query.Insert(new Customer
            {
                Id = Id,
                Name = Name,
                Email = Email
            });

            var customer = query.FirstOrDefault();

            Assert.IsNotNull(customer, "Customer failed");
            Assert.IsNull(customer.Address, "Address failed");
            Assert.AreEqual(Id, customer.Id, "Customer Id failed");
            Assert.AreEqual(Name, customer.Name, "Customer Name failed");
            Assert.AreEqual(Email, customer.Email, "Customer Email failed");
        }

        [TestMethod]
        public void SelectDistinct()
        {
            const int Id = 1;
            const string Name = "User 1";
            using var query = NewQuery();

            query.Insert(NewRow(Id, Name).Cells);
            query.Distinct = true;

            var row = query.FirstRow();

            Assert.IsNotNull(row);
            Assert.AreEqual(Id, row[ID]);
            Assert.AreEqual(Name, row[NAME]);
        }

        [TestMethod]
        public void SelectByValidPk()
        {
            const int Id = 1;
            const string Name = "User 1";
            using var query = new Query<TestTable>(Creator);

            query.Insert(new TestTable
            {
                Id = Id,
                Id2 = Id,
                Name = Name
            });

            var value = query.Find(Id, Id);
            Assert.IsNotNull(value);
        }

        [TestMethod]
        public void SelectGroupBy()
        {
            const string Name = "User 1";
            using var query = new Query<TestTable>(Creator);
            query.Delete();

            query.BulkInsert(
                new TestTable { Id = 1, Id2 = 1, Name = Name },
                new TestTable { Id = 2, Id2 = 1, Name = Name },
                new TestTable { Id = 3, Id2 = 2, Name = Name },
                new TestTable { Id = 4, Id2 = 2, Name = Name }
            );

            query.GroupBy("Id2");
            Assert.AreEqual(2, query.Count());
        }

        [TestMethod]
        public void SelectGroupByHaving()
        {
            const string Name = "User 1";
            using var query = new Query<TestTable>(Creator);
            query.Delete();

            query.BulkInsert(
                new TestTable { Id = 1, Id2 = 1, Name = Name },
                new TestTable { Id = 2, Id2 = 2, Name = Name },
                new TestTable { Id = 3, Id2 = 3, Name = Name },
                new TestTable { Id = 4, Id2 = 2, Name = Name }
            );

            query.GroupBy("Id2");
            query.Having(q => q.Where("Id2", "=", 1));
            Assert.AreEqual(1, query.Count());
        }

        [TestMethod]
        public void Update()
        {
            const int Id = 1;
            using var query = NewQuery();
            query.Insert(NewRow(Id, "Name1").Cells);

            query.Where(ID, Id);
            query.Update(new Cell(NAME, "Name2"));

            var row = query.FirstRow();
            Assert.IsNotNull(row);
            Assert.AreEqual("Name2", row[NAME]);
        }

        [TestMethod]
        public void UpdateObject()
        {
            const int Id = 1;
            using var query = new Query<TestTable>(Creator);
            query.Insert(new TestTable
            {
                Id = Id,
                Name = "Name1"
            });

            query.Where(ID, Id);
            query.Update(new TestTable
            {
                Id = Id,
                Name = "Name2"
            });

            query.Update(new TestTable
            {
                Nick = "Tester"
            }, NICK);

            var row = query.FirstRow();
            Assert.IsNotNull(row);
            Assert.AreEqual("Name2", row[NAME]);
            Assert.AreEqual("Tester", row[NICK]);
            Assert.ThrowsException<InvalidOperationException>(() => query.Update(new TestTable(), "Invalid"));
        }

        [TestMethod]
        public void Delete()
        {
            using var query = NewQuery();
            query.Delete();
            query.BulkInsert(NewRow(1, "A"), NewRow(2, "B"));

            Assert.AreEqual(2, query.Count());

            query.Where(NAME, "B");
            query.Delete();

            using var query2 = NewQuery();
            Assert.AreEqual(1, query2.Count());
        }

        [TestMethod]
        public void Upsert()
        {
            using var query = NewQuery();

            query.Upsert(NewRow(1, "A1"), new[] { ID });
            Assert.AreEqual(1, query.FirstRow()[ID]);

            query.Upsert(NewRow(1, "Josh"), new[] { ID });
            Assert.AreEqual("Josh", query.FirstRow()[NAME]);
        }

        [TestMethod]
        public void UpdateWhereIsNull()
        {
            const int Id = 1;
            using var query = NewQuery();
            query.Insert(NewRow(Id, "Name1").Cells);

            query.Where(NICK, null);
            bool changed = query.Update(new Cell(NICK, "Name2")) > 0;
            Assert.IsTrue(changed);

            using var query2 = NewQuery();
            query2.Where(ID, Id);

            var row = query2.FirstRow();
            Assert.IsNotNull(row);
            Assert.AreEqual("Name2", row[NICK]);
        }

        [TestMethod]
        public void DeleteWhereJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Creator);
            using var qCustomer = new Query<Customer>(Creator);

            qOrder.Join<Customer>("c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Delete();

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            Assert.AreEqual(2, qOrder.Count());

            var order = qOrder.FirstOrDefault();
            Assert.AreEqual("Ok", order.Status);
        }

        [TestMethod]
        public void UpdateJoin()
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
            Assert.AreEqual("Ok", order.Status);

            qOrder.Offset = null;
            order = qOrder.FirstOrDefault();
            Assert.AreEqual("Processed", order.Status);
        }

        [TestMethod]
        public void SelectWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(Creator);
            query.AddForeign(f => f.Customer).AddForeign(f => f.Customer.Address);
            var order = query.FirstOrDefault();

            Assert.IsNotNull(order.Customer);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);

            Assert.IsNotNull(order.Customer.Address);
            Assert.AreEqual(order.Customer.AddressId, order.Customer.Address.Id);
        }

        [TestMethod]
        public void FindWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(Creator);
            var order = query.AddForeign(o => o.Customer.Address).Find(1);

            Assert.IsNotNull(order.Customer);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);

            Assert.IsNotNull(order.Customer.Address);
            Assert.AreEqual(order.Customer.AddressId, order.Customer.Address.Id);
        }

        [TestMethod]
        public void ExecuteArrayScalar()
        {
            using var q = NewQuery();
            q.Delete();
            InsertRows(5);
            q.Select("Name");

            var names = q.ExecuteArrayScalar<string>();
            Assert.AreEqual(5, names.Length);
        }

        [TestMethod]
        public void ExecuteArrayScalarConvertion()
        {
            InsertRows(5);
            using var q = NewQuery();
            q.Select("Id");

            var names = q.ExecuteArrayScalar<string>();
            Assert.AreEqual(5, names.Length);
        }

        [TestMethod]
        public void PaginateWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(Creator);
            var orders = ((Query<Order>)query.Where("customer_id", 2)).AddForeign(o => o.Customer.Address).Paginate(2, 1);

            Assert.IsNotNull(orders[0].Customer);
            Assert.AreEqual(orders[0].Customer, orders[1].Customer);
            Assert.AreEqual(orders[0].CustomerId, orders[1].CustomerId);
            Assert.AreEqual(orders[0].Customer.Id, orders[1].Customer.Id);

            Assert.IsNotNull(orders[0].Customer.Address);
        }

        [TestMethod]
        public void DateUtcConversion()
        {
            using var query1 = new Query<TestTable>(new ConnectionManager(new MysqlQueryConfig
            {
                Translation = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc }
            }, Creator.GetConnection()));

            DateTime date = DateTime.Now.RemoveMiliseconds();
            query1.Insert(new TestTable { Id = 1, Name = "", CreatedAt = date, Number = 0, CustomStatus = Status.Success });
            DateTime dbDate = query1.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.AreEqualsDate(date, dbDate, "Universal time insert fail");

            using var query2 = new Query<TestTable>(Creator);
            dbDate = query2.Select("record_created").ExecuteScalar<DateTime>();
            TestAssert.AreEqualsDate(date.ToUniversalTime(), dbDate, "Universal time read file");
        }

        public void ConfigureInitialCustomerAndOrder()
        {
            using var qOrder = new Query<Order>(Creator);
            using var qAddress = new Query<Address>(Creator);
            using var qCustomer = new Query<Customer>(Creator);
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

        [TestMethod]
        public void TryLoadNullInt()
        {
            using var qOrder = new Query<Order>(Creator);
            qOrder.Where("Id", 0);
            Assert.AreEqual(0, qOrder.ExecuteScalar<int>());
            Assert.IsNull(qOrder.ExecuteScalar<int?>());
        }

        [TestCleanup]
        public void ClearDbTest()
        {
            using var query = NewQuery();
            query.Delete();
        }
    }
}
