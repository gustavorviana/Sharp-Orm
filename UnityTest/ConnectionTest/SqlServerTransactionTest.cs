using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using UnityTest.Utils;

namespace UnityTest.ConnectionTest
{
    [TestClass]
    public class SqlServerTransactionTest : SqlServerTest
    {
        [TestMethod]
        [TestProperty("clearDb", "")]
        public void Select()
        {
            ConnectionCreator.Default = this.Creator;
            try
            {
                ConnectionCreator.ExecuteTransaction((transaction) =>
                {
                    var manager = new ConnectionManager(Config, transaction);
                    using var q = new Query(TABLE, manager);
                    q.Insert(NewRow(1, "User 1").Cells);

                    using var qSelect = new Query(TABLE, manager);

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
    }
}
