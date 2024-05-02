using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class MysqlPaginationTest : MysqlTableTest
    {
        [TestMethod]
        public void Paginate()
        {
            InsertRows(10);

            using var query = NewQuery();
            using var pager = query.PaginateRows(8, 1);

            Assert.IsNotNull(pager);
            Assert.AreEqual(8, pager.Count);
            Assert.AreEqual(2, pager.Pages);
            Assert.AreEqual(1, pager.CurrentPage);
            Assert.AreEqual(10, pager.Total);
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
        public void PaginateOrderBy()
        {
            InsertRows(10);

            using var query = NewQuery();
            query.OrderBy("Id");
            using var pager = query.PaginateRows(8, 1);

            Assert.IsNotNull(pager);
            Assert.AreEqual(8, pager.Count);
            Assert.AreEqual(2, pager.Pages);
            Assert.AreEqual(1, pager.CurrentPage);
            Assert.AreEqual(10, pager.Total);
        }

        [TestMethod]
        public void GotoPage()
        {
            InsertRows(10);

            using var query = NewQuery();
            using var pager = query.PaginateRows(8, 1);
            pager.GoToPage(2);

            Assert.IsNotNull(pager);
            Assert.AreEqual(2, pager.Count);
            Assert.AreEqual(2, pager.CurrentPage);
            Assert.AreEqual(10, pager.Total);
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
