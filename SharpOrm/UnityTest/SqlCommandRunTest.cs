﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class SqlCommandRunTest : MysqlTableTest
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
            bool changed = query.Update(new Cell(NICK, "Name2"));
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
