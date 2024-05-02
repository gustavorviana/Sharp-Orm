using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServeCommandRunTest : SqlServerTest
    {
        [TestMethod]
        public void SelectDistinct()
        {
            const int Id = 1;
            const string Name = "User 1";

            using var q = new Query<TestTable>(Creator);
            q.Delete();
            q.Insert(NewRow(Id, Name).Cells);
            q.Distinct = true;

            var row = q.FirstRow();

            Assert.IsNotNull(row);
            Assert.AreEqual(Id, row[ID]);
            Assert.AreEqual(Name, row[NAME]);
        }

        [TestMethod]
        public void MultipleSelectCount()
        {
            using var q = new Query<TestTable>(Creator);
            q.Delete();
            InsertRows(4);
            q.Select(ID, NAME, NICK);

            Assert.AreEqual(4, q.Count());
            Assert.AreEqual(4, q.Count("*"));
        }

        [TestMethod]
        public void PaginateDistinct()
        {
            InsertRows(4);

            using var q = new Query<TestTable>(Creator);
            q.Insert(NewRow(6, "User 1").Cells);
            q.OrderBy(NAME);
            q.Distinct = true;
            q.Select(NAME);
            var r = q.Paginate(5, 1);

            Assert.IsNotNull(r);
            Assert.AreEqual(4, r.Count);
            Assert.AreEqual(4, r.Total);
        }

        [TestMethod]
        public void PaginateWithoutOrderby()
        {
            using var q = new Query<TestTable>(Creator);
            Assert.ThrowsException<InvalidOperationException>(() => q.Paginate(1, 2));
        }

        [TestMethod]
        [TestProperty("clearDb", "")]
        public void Paginate()
        {
            InsertRows(30);

            using var q = new Query<TestTable>("p", Creator);
            q.Select("p.*");
            q.OrderBy("Id");
            var r = q.Paginate(5, 1);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Count);
            Assert.AreEqual(30, r.Total);
            Assert.AreEqual(1, r.CurrentPage);
        }

        [TestMethod]
        public void DeleteWhereJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(this.Creator);

            qOrder.Join<Customer>("c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Delete();

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            Assert.AreEqual(1, qOrder.Count());

            var order = qOrder.FirstOrDefault();
            Assert.AreEqual("Ok", order.Status);
        }

        [TestMethod]
        public void SelectJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(this.Creator);

            qOrder.Join<Customer>("c", q => q.WhereColumn("c.id", "orders.customer_id").Where("c.Email", "!=", "Test"));
            qOrder.Where("c.name", "Ronaldo");
            Assert.IsNotNull(qOrder.FirstOrDefault());
        }

        [TestMethod]
        public void SelectGroupBy()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(this.Creator);
            query.BulkInsert(
                new Order
                {
                    Id = 3,
                    CustomerId = 1,
                    Product = "My product",
                    Quantity = 5,
                    Status = "Pending"
                },
                new Order
                {
                    Id = 4,
                    CustomerId = 2,
                    Product = "My product 2",
                    Quantity = 5,
                    Status = "Ok"
                }
            );

            query.GroupBy("Status", "customer_id");
            Assert.AreEqual(2, query.Count());
        }

        [TestMethod]
        public void SelectGroupByHaving()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(this.Creator);
            query.BulkInsert(
                new Order
                {
                    Id = 3,
                    CustomerId = 1,
                    Product = "My product",
                    Quantity = 5,
                    Status = "Pending"
                },
                new Order
                {
                    Id = 4,
                    CustomerId = 2,
                    Product = "My product 2",
                    Quantity = 5,
                    Status = "Ok"
                }
            );

            query.GroupBy("Status", "customer_id");
            query.Having(q => q.Where("Status", "Ok"));
            Assert.AreEqual(2, query.Count());
        }

        [TestMethod]
        public void UpdateJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(this.Creator);

            qOrder.Join<Customer>("c", "c.id", "orders.customer_id");
            qOrder.Where("c.name", "Ronaldo");
            qOrder.Update(new Cell("Status", "Processed"));

            qOrder.Info.Joins.Clear();
            qOrder.Info.Where.Clear();

            qOrder.OrderBy("Id");
            qOrder.Offset = 1;
            var order = qOrder.FirstOrDefault();
            Assert.AreEqual("Ok", order.Status);

            qOrder.Offset = null;
            order = qOrder.FirstOrDefault();
            Assert.AreEqual("Processed", order.Status);
        }

        [TestMethod]
        [TestProperty("clearDb", "")]
        public void PaginatePage2()
        {
            InsertRows(10);

            using var q = new Query<TestTable>(Creator);
            q.OrderBy("Id");
            var r = q.Paginate(8, 2);

            Assert.IsNotNull(r);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(10, r.Total);
            Assert.AreEqual(2, r.CurrentPage);
        }

        [TestMethod]
        public void SelectWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(this.Creator);
            var order = query.AddForeign(o => o.Customer).FirstOrDefault();

            Assert.IsNotNull(order.Customer);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);
        }

        [TestMethod]
        public void SelectWithNoLock()
        {
            const int Id = 1;
            const string Name = "User 1";

            using var q = new Query<TestTable>(Creator);
            q.Delete();

            q.EnableNoLock();
            q.Insert(NewRow(Id, Name).Cells);

            var row = q.FirstRow();

            Assert.IsNotNull(row);
            Assert.AreEqual(Id, row[ID]);
            Assert.AreEqual(Name, row[NAME]);
        }

        [TestMethod]
        public void DeleteWithNoLock()
        {
            using var q = new Query<TestTable>("t", this.Creator);
            q.EnableNoLock();
            q.Delete();
        }

        [TestMethod]
        public void UpdateNoLock()
        {
            using var q = new Query<TestTable>("t", this.Creator);
            q.EnableNoLock();
            q.Where("Id", 0);
            q.Update(new Cell("Name", "test"));
        }

        private void ConfigureInitialCustomerAndOrder()
        {
            using var qOrder = new Query<Order>(this.Creator);
            using var qAddress = new Query<Address>(this.Creator);
            using var qCustomer = new Query<Customer>(this.Creator);
            qOrder.Delete();
            qCustomer.Delete();
            qAddress.Delete();

            qAddress.BulkInsert(
                new Address
                {
                    Id = 1,
                    Name = "Test 1",
                    Street = "Street 1"
                },
                new Address
                {
                    Id = 2,
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
                }
            );
        }
    }
}