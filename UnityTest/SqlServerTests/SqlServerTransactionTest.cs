using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.Data;
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
        public void TransactionType()
        {
            ConnectionCreator.Default = Creator;
            using (var manager = new ConnectionManager())
                Assert.IsNull(manager.Transaction);

            using (var manager = new ConnectionManager(true))
                Assert.IsNotNull(manager.Transaction);

            using (var manager = new ConnectionManager(IsolationLevel.Serializable))
            {
                Assert.IsNotNull(manager.Transaction);
                Assert.AreEqual(IsolationLevel.Serializable, manager.Transaction.IsolationLevel);
                Assert.AreEqual("Serializable", manager.ExecuteScalar<string>(GetTransactionLevelExp()));
            }
        }

        private static SqlExpression GetTransactionLevelExp()
        {
            return (SqlExpression)@"
                    SELECT CASE transaction_isolation_level
                        WHEN 0 THEN 'Unspecified'
                        WHEN 1 THEN 'Read Uncommitted'
                        WHEN 2 THEN 'Read Committed'
                        WHEN 3 THEN 'Repeatable Read'
                        WHEN 4 THEN 'Serializable'
                        WHEN 5 THEN 'Snapshot'
                        ELSE 'Unknown'
                    END AS IsolationLevelDescription
                    FROM sys.dm_exec_sessions
                    WHERE session_id = @@SPID";
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

        [TestMethod]
        public void AutoOpenConnectionTest()
        {
            using var localCreator = new SingleConnectionCreator(new SqlServerQueryConfig(), ConnectionStr.SqlServer) { AutoOpenConnection = true };
            using var conn = localCreator.GetConnection();
            Assert.IsTrue(conn.IsOpen());

            using var localCreator2 = new SingleConnectionCreator(new SqlServerQueryConfig(), ConnectionStr.SqlServer);
            using var conn2 = localCreator2.GetConnection();
            Assert.IsFalse(conn2.IsOpen());
        }

        [TestMethod]
        public void ManagementTest()
        {
            using var localCreator = new SingleConnectionCreator(new SqlServerQueryConfig(), ConnectionStr.SqlServer) { Management = ConnectionManagement.LeaveOpen };
            using var manager = new ConnectionManager(localCreator);
            Assert.AreEqual(ConnectionManagement.LeaveOpen, manager.Management);
        }
    }
}
