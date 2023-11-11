using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using UnityTest.Utils;

namespace UnityTest.ConnectionTest
{
    [TestClass]
    public class MysqlTransactionTest : MysqlTableTest
    {
        [TestMethod]
        public void Select()
        {
            try
            {
                ConnectionCreator.ExecuteTransaction((transaction) =>
                {
                    using var q = new Query(transaction, TABLE);
                    q.Insert(NewRow(1, "User 1").Cells);

                    using var qSelect = new Query(transaction, TABLE);

                    Assert.AreEqual(1, qSelect.Count());
                    throw new DatabaseException();
                });
            }
            catch (DatabaseException)
            {
                using var qSelect = new Query(TABLE);

                Assert.AreEqual(0, qSelect.Count());
            }
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
