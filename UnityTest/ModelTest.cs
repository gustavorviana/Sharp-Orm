using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class ModelTest : MysqlTableTest
    {
        [TestMethod]
        public void LoadFromTableModelClass()
        {
            InsertRows(1);

            using var q = new Query<TestModelTable>(Connection);
            q.Where(ID, 1);

            var model = q.FirstOrDefault();
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("User 1", model.Name);
            Assert.IsNull(model.Nick);
            Assert.IsInstanceOfType(model.CreatedAt, typeof(DateTime));
        }

        [TestMethod]
        public void LoadFromTableClass()
        {
            InsertRows(1);

            using var q = new Query<TestTable>(Connection);
            q.Where(ID, 1);

            var model = q.FirstOrDefault();
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("User 1", model.Name);
            Assert.IsNull(model.Nick);
            Assert.IsInstanceOfType(model.CreatedAt, typeof(DateTime));
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
