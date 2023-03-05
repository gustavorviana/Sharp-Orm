using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
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
                Name = Name
            });

            var value = query.Find(Id);
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

        [TestCleanup]
        [TestInitialize]
        public void CleanupTest()
        {
            using var query = NewQuery();
            query.Delete();
        }
    }
}
