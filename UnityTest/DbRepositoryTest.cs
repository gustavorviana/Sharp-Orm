using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System.Data.Common;
using System.Reflection;
using UnityTest.Models;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class DbRepositoryTest
    {
        private static readonly FieldInfo _transactionField = typeof(DbRepository).GetField("_transaction", BindingFlags.Instance | BindingFlags.NonPublic);
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
                var transaction = GetTransaction(repo);
                Assert.IsNotNull(transaction);

                repo.RunTransaction(() => Assert.AreEqual(transaction, GetTransaction(repo)));
                Assert.IsNotNull(GetTransaction(repo));
            });

            Assert.IsNull(GetTransaction(repo));
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

                Assert.AreEqual(transaction, GetTransaction(repo));
                repo.RunTransaction(() => Assert.AreEqual(transaction, GetTransaction(repo)));

                Assert.AreEqual(transaction, GetTransaction(repo));

                repo.SetTransaction((DbTransaction)null);
                Assert.IsNull(GetTransaction(repo));
            }
            finally
            {
                creator.SafeDisposeConnection(conn);
            }
        }

        private static DbTransaction GetTransaction(TestRepository repo)
        {
            return ((ConnectionManager)_transactionField.GetValue(repo))?.Transaction;
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

            Assert.IsNull(query.Transaction);

            repo.BeginTransaction();
            query = repo.GetQuery();
            Assert.IsNotNull(query.Transaction);

            repo.CommitTransaction();
            query = repo.GetQuery();
            Assert.IsNull(query.Transaction);
        }
    }
}
