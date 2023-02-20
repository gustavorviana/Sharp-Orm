using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using UnityTest.Utils;

namespace UnityTest
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
