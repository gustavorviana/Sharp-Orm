using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerTransactionTest : SqlServerTest
    {
        [TestMethod]
        public void Select()
        {
            ConnectionCreator.Default = this.Creator;
            this.ClearTable(TABLE);
            try
            {
                ConnectionManager.ExecuteTransaction((manager) =>
                {
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

        [TestMethod]
        public void ManagerTrasactionTest()
        {
            ConnectionCreator.Default = Creator;
            this.ClearTable(TABLE);
            using (var manager = new ConnectionManager(true))
            {
                using var q = new Query(TABLE, manager);
                q.Insert(NewRow(1, "User 1").Cells);

                using var qSelect = new Query(TABLE, manager);
                Assert.AreEqual(1, qSelect.Count());

                manager.Rollback();
            }

            using var qSelect2 = new Query(TABLE);
            Assert.AreEqual(0, qSelect2.Count());
        }

        [TestMethod]
        public void OpenTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var noTransaction = new ConnectionManager(false);
            Assert.AreEqual(ConnectionManagement.CloseOnEndOperation, noTransaction.Management);
            Assert.IsNotNull(noTransaction.Connection);
            Assert.IsNull(noTransaction.Transaction);

            using var transaction = noTransaction.BeginTransaction();
            Assert.IsTrue(transaction.isMyTransaction);
            Assert.AreEqual(ConnectionManagement.CloseOnDispose, transaction.Management);
            Assert.IsNotNull(transaction.Connection);
            Assert.IsNotNull(transaction.Transaction);
        }

        [TestMethod]
        public void GetOpenTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var transaction = new ConnectionManager(true);
            Assert.AreEqual(ConnectionManagement.CloseOnDispose, transaction.Management);
            Assert.IsTrue(transaction.isMyTransaction);
            Assert.IsNotNull(transaction.Connection);
            Assert.IsNotNull(transaction.Transaction);
        }

        [TestMethod]
        public void UseExistingTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var conn = Creator.GetConnection();
            using var transaction = new ConnectionManager(Creator.Config, conn.OpenIfNeeded().BeginTransaction());
            Assert.AreEqual(ConnectionManagement.LeaveOpen, transaction.Management);
            Assert.IsFalse(transaction.isMyTransaction);
            Assert.IsNotNull(transaction.Connection);
            Assert.IsNotNull(transaction.Transaction);
        }
    }
}
