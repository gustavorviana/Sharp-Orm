using BaseTest.Utils;
using DbRunTest.Fixtures;
using DbRunTest.Utils;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System.Data;
using System.Data.SqlClient;

namespace DbRunTest
{
    public class TransactionTest() : DbTestBase(new DbFixture<SqlConnection>())
    {
        [Fact]
        public void Select()
        {
            try
            {
                ConnectionManager.ExecuteTransaction((manager) =>
                {
                    using var q = NewQuery(manager, TestTableUtils.TABLE);
                    q.Insert(TestTableUtils.NewRow(1, "User 1").Cells);

                    using var qSelect = NewQuery(manager, TestTableUtils.TABLE);

                    Assert.Equal(1, qSelect.Count());
                    throw new DatabaseException();
                });
            }
            catch (DatabaseException)
            {
                using var qSelect = NewQuery(TestTableUtils.TABLE);

                Assert.Equal(0, qSelect.Count());
            }
        }

        [Fact]
        public void ManagerTrasactionTest()
        {
            using (var manager = new ConnectionManager(true))
            {
                using var q = NewQuery(manager, TestTableUtils.TABLE);
                q.Insert(TestTableUtils.NewRow(1, "User 1").Cells);

                using var qSelect = NewQuery(manager, TestTableUtils.TABLE);
                Assert.Equal(1, qSelect.Count());

                manager.Rollback();
            }

            using var qSelect2 = NewQuery(TestTableUtils.TABLE);
            Assert.Equal(0, qSelect2.Count());
        }

        [Fact]
        public void TransactionType()
        {
            ConnectionCreator.Default = Creator;
            using (var manager = new ConnectionManager())
                Assert.Null(manager.Transaction);

            using (var manager = new ConnectionManager(true))
                Assert.NotNull(manager.Transaction);

            using (var manager = new ConnectionManager(IsolationLevel.Serializable))
            {
                Assert.NotNull(manager.Transaction);
                Assert.Equal(IsolationLevel.Serializable, manager.Transaction.IsolationLevel);
                Assert.Equal("Serializable", manager.ExecuteScalar<string>(GetTransactionLevelExp()));
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

        [Fact]
        public void OpenTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var noTransaction = new ConnectionManager(false);
            Assert.Equal(ConnectionManagement.CloseOnEndOperation, noTransaction.Management);
            Assert.NotNull(noTransaction.Connection);
            Assert.Null(noTransaction.Transaction);

            using var transaction = noTransaction.BeginTransaction();
            Assert.True(transaction.isMyTransaction);
            Assert.Equal(ConnectionManagement.CloseOnDispose, transaction.Management);
            Assert.NotNull(transaction.Connection);
            Assert.NotNull(transaction.Transaction);
        }

        [Fact]
        public void GetOpenTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var transaction = new ConnectionManager(true);
            Assert.Equal(ConnectionManagement.CloseOnDispose, transaction.Management);
            Assert.True(transaction.isMyTransaction);
            Assert.NotNull(transaction.Connection);
            Assert.NotNull(transaction.Transaction);
        }

        [Fact]
        public void UseExistingTransactionTest()
        {
            ConnectionCreator.Default = Creator;
            using var conn = Creator.GetConnection();
            using var transaction = new ConnectionManager(Creator.Config, conn.OpenIfNeeded().BeginTransaction());
            Assert.Equal(ConnectionManagement.LeaveOpen, transaction.Management);
            Assert.False(transaction.isMyTransaction);
            Assert.NotNull(transaction.Connection);
            Assert.NotNull(transaction.Transaction);
        }

        [Fact]
        public void AutoOpenConnectionTest()
        {
            var init = ConnectionMap.Get(typeof(SqlConnection));

            using var localCreator = new SingleConnectionCreator(init.GetConfig(), init.ConnString) { AutoOpenConnection = true };
            using var conn = localCreator.GetConnection();
            Assert.True(conn.IsOpen());

            using var localCreator2 = new SingleConnectionCreator(init.GetConfig(), init.ConnString);
            using var conn2 = localCreator2.GetConnection();
            Assert.False(conn2.IsOpen());
        }

        [Fact]
        public void ManagementTest()
        {
            var init = ConnectionMap.Get(typeof(SqlConnection));

            using var localCreator = new SingleConnectionCreator(init.GetConfig(), init.ConnString) { Management = ConnectionManagement.LeaveOpen };
            using var manager = new ConnectionManager(localCreator);
            Assert.Equal(ConnectionManagement.LeaveOpen, manager.Management);
        }
    }
}
