using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System.Data.Common;
using UnityTest.Models;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class DbRepositoryTest
    {
        private static readonly ConnectionCreator creator = new MultipleConnectionCreator<MockConnection>(new MysqlQueryConfig(), "");

        [TestMethod]
        public void MultipleCommit()
        {
            using var repo = new TestRepository(creator);

            repo.BeginTransaction();
            Assert.ThrowsException<DatabaseException>(() => repo.BeginTransaction());

            repo.CommitTransaction();
            Assert.ThrowsException<DatabaseException>(() => repo.CommitTransaction());
        }

        [TestMethod]
        public void MultipleRollback()
        {
            using var repo = new TestRepository(creator);

            repo.BeginTransaction();
            Assert.ThrowsException<DatabaseException>(() => repo.BeginTransaction());

            repo.RollbackTransaction();
            Assert.ThrowsException<DatabaseException>(() => repo.RollbackTransaction());
        }

        [TestMethod]
        public void MultipleCallBack()
        {
            using var repo = new TestRepository(creator);
            repo.RunTransaction(() =>
            {
                var transaction = repo.GetTransaction();
                Assert.IsNotNull(transaction);

                repo.RunTransaction(() => Assert.AreEqual(transaction, repo.GetTransaction()));
                Assert.IsNotNull(repo.GetTransaction());
            });

            Assert.IsNull(repo.GetTransaction());
        }

        [TestMethod]
        public void ExternalTransaction()
        {
            var conn = creator.GetConnection();
            try
            {
                using var transaction = conn.BeginTransaction();
                using var repo = new TestRepository(creator);
                repo.SetTransaction(transaction);

                Assert.AreEqual(transaction, repo.GetTransaction());
                repo.RunTransaction(() => Assert.AreEqual(transaction, repo.GetTransaction()));

                Assert.AreEqual(transaction, repo.GetTransaction());

                repo.SetTransaction((DbTransaction)null);
                Assert.IsNull(repo.GetTransaction());
            }
            finally
            {
                creator.SafeDisposeConnection(conn);
            }
        }

        [TestMethod]
        public void CreateCommandWithTransaction()
        {
            using var repo = new TestRepository(creator);
            repo.RunTransaction(() =>
            {
                using var cmd1 = repo.CreateCommand("");
                Assert.IsNotNull(cmd1.Transaction);

                using var cmd2 = repo.CreateCommand("", System.Array.Empty<object>());
                Assert.IsNotNull(cmd2.Transaction);
            });
        }

        [TestMethod]
        public void QueryTransaction()
        {
            using var repo = new TestRepository(creator);
            var query = repo.GetQuery();

            Assert.IsNull(query.Manager.Transaction);

            repo.BeginTransaction();
            query = repo.GetQuery();
            Assert.IsNotNull(query.Manager.Transaction);

            repo.CommitTransaction();
            query = repo.GetQuery();
            Assert.IsNull(query.Manager.Transaction);
        }

        [TestMethod]
        public void ConnectionDisposeCleanTest()
        {
            using var repo = new TestRepository(creator);

            for (int i = 0; i < 5; i++)
                repo.GetConnection().Dispose();

            Assert.AreEqual(0, repo._connections.Count);
        }
    }
}
