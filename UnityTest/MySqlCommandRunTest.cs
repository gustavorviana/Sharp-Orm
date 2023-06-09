using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlX.XDevAPI.Relational;
using SharpOrm;
using System;
using System.ComponentModel.DataAnnotations;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class MySqlCommandRunTest : MysqlTableTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery();
            q.Insert(NewRow(1, "User 1").Cells);

            Assert.AreEqual(1, q.Count());
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
            using var query = new Query<TestTable>();

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
            using var query = new Query<TestTable>();
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
        public void DeleteJoin()
        {
            using var qOrder = new Query<Order>(Connection);
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

            qCustomer.Insert(new Customer
            {
                Id = 2,
                Name = "Michael",
                Address = "My address 2",
                Email = "michael@email.com"
            });

            qOrder.Insert(new Order
            {
                Id = 1,
                CustomerId = 1,
                Product = "My product",
                Quantity = 10,
                Status = "Pending"
            });
            qOrder.Insert(new Order
            {
                Id = 2,
                CustomerId = 2,
                Product = "My product 2",
                Quantity = 10,
                Status = "Ok"
            });

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
            using var qOrder = new Query<Order>(Connection);
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

            qCustomer.Insert(new Customer
            {
                Id = 2,
                Name = "Michael",
                Address = "My address 2",
                Email = "michael@email.com"
            });

            qOrder.Insert(new Order
            {
                Id = 1,
                CustomerId = 1,
                Product = "My product",
                Quantity = 10,
                Status = "Pending"
            });
            qOrder.Insert(new Order
            {
                Id = 2,
                CustomerId = 2,
                Product = "My product 2",
                Quantity = 10,
                Status = "Ok"
            });

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
            using var query = new Query<Order>();
            var o = query.FirstOrDefault();

            Assert.IsNotNull(o.Customer);
        }

        [TestCleanup]
        [TestInitialize]
        public void CleanupTest()
        {
            using var query = NewQuery();
            query.Delete();
        }
    }
}
