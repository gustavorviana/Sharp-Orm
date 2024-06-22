using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerBulkOperationTest : SqlServerTest
    {
        [TestMethod]
        public void UpdateTest()
        {
            var _guid = Guid.NewGuid();
            using var manager = GetConnectionManager();
            InsertRows(5);

            using var q = new Query<TestTable>(manager);
            int updateCount = QueryExtension.BulkUpdate(q, new Row[] { MakeUpdateRow(1, _guid), MakeUpdateRow(2, _guid), MakeUpdateRow(3, _guid) }, new string[] { ID, NAME });
            q.Where(GUIDID, _guid);

            Assert.AreEqual(3, updateCount);
            Assert.AreEqual(3, q.Count());
        }

        private static Row MakeUpdateRow(int id, Guid guid)
        {
            return new Row(
                new Cell(ID, id),
                new Cell(NAME, $"User {id}"),
                new Cell(NUMBER, id),
                new Cell(GUIDID, guid.ToString()),
                new Cell(STATUS, 1)
            );
        }

        [TestMethod]
        public void DeleteTest()
        {
            using var manager = GetConnectionManager();
            InsertRows(5);

            using var q = new Query<TestTable>(manager);
            int deleteCount = QueryExtension.BulkDelete(q, new Row[] { MakeDeleteRow(1), MakeDeleteRow(2), MakeDeleteRow(3) });

            Assert.AreEqual(3, deleteCount);
            Assert.AreEqual(2, q.Count());
        }

        private static Row MakeDeleteRow(int number)
        {
            return new Row(
                new Cell(NAME, $"User {number}"),
                new Cell(NUMBER, number)
            );
        }

        [TestInitialize()]
        public void Startup()
        {
            using var manager = GetConnectionManager();
            using var q = new Query<TestTable>(manager);
            q.Delete();
        }
    }
}
