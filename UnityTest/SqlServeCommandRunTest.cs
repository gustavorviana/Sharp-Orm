using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Data.SqlClient;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class SqlServeCommandRunTest : SqlServerTest
    {
        protected static readonly SqlServerQueryConfig newConfig = new(false) { UseOldPagination = false };
        protected static readonly ConnectionCreator Creator = new SingleConnectionCreator<SqlConnection>(newConfig, ConnectionStr.SqlServer);

        [TestMethod]
        public void SelectDistinct()
        {
            const int Id = 1;
            const string Name = "User 1";

            var q = new Query<TestTable>(Creator);
            q.Insert(NewRow(Id, Name).Cells);
            q.Distinct = true;

            var row = q.FirstRow();

            Assert.IsNotNull(row);
            Assert.AreEqual(Id, row[ID]);
            Assert.AreEqual(Name, row[NAME]);
        }

        [TestMethod]
        public void PaginateDistinct()
        {
            InsertRows(4);

            var q = new Query<TestTable>(Creator);
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
            var q = new Query<TestTable>(Creator);
            Assert.ThrowsException<InvalidOperationException>(() => q.Paginate(1, 2));
        }

        [TestMethod]
        [TestProperty("clearDb", "")]
        public void Paginate()
        {
            InsertRows(30);

            var q = new Query<TestTable>(Creator, "p");
            q.Select("p.*");
            q.OrderBy("Id");
            var r = q.Paginate(5, 1);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Count);
            Assert.AreEqual(30, r.Total);
            Assert.AreEqual(1, r.CurrentPage);
        }

        [TestMethod]
        public void DeleteJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Connection);
            using var qCustomer = new Query<Customer>(Connection);

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
        public void UpdateJoin()
        {
            ConfigureInitialCustomerAndOrder();
            using var qOrder = new Query<Order>(Connection);
            using var qCustomer = new Query<Customer>(Connection);

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

            var q = new Query<TestTable>(Creator);
            q.OrderBy("Id");
            var r = q.Paginate(8, 2);

            Assert.IsNotNull(r);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(10, r.Total);
            Assert.AreEqual(2, r.CurrentPage);
        }


        [TestMethod]
        public void InsertWithForeign()
        {
            using var qOrder = new Query<Order2>(Connection);
            using var qCustomer = new Query<Customer>(Connection);
            qOrder.Delete();
            qCustomer.Delete();

            qCustomer.Insert(new Customer
            {
                Id = 1,
                Name = "Ronaldo",
                Address = "My address",
                Email = "ronaldo@email.com"
            });

            qOrder.Insert(new Order2
            {
                Id = 1,
                Customer = new Customer { Id = 1 },
                Product = "My product",
                Quantity = 10,
                Status = "Pending"
            });
        }

        [TestMethod]
        public void SelectWithForeign()
        {
            ConfigureInitialCustomerAndOrder();
            using var query = new Query<Order>(Connection);
            var order = query.WithAllForeigns().FirstOrDefault();

            Assert.IsNotNull(order.Customer);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);
        }

        private static void ConfigureInitialCustomerAndOrder()
        {
            using var qOrder = new Query<Order>(Connection);
            using var qCustomer = new Query<Customer>(Connection);
            qOrder.Delete();
            qCustomer.Delete();

            qCustomer.BulkInsert(
                new Customer
                {
                    Id = 1,
                    Name = "Ronaldo",
                    Address = "My address",
                    Email = "ronaldo@email.com"
                },
                new Customer
                {
                    Id = 2,
                    Name = "Michael",
                    Address = "My address 2",
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