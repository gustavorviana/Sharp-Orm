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
                var transaction = _transactionField.GetValue(repo);
                Assert.IsNotNull(transaction);

                repo.RunTransaction(() => Assert.AreEqual(transaction, _transactionField.GetValue(repo)));
                Assert.IsNotNull(_transactionField.GetValue(repo));
            });

            Assert.IsNull(_transactionField.GetValue(repo));
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

                Assert.AreEqual(transaction, _transactionField.GetValue(repo));
                repo.RunTransaction(() => Assert.AreEqual(transaction, _transactionField.GetValue(repo)));

                Assert.AreEqual(transaction, _transactionField.GetValue(repo));

                repo.SetTransaction((DbTransaction)null);
                Assert.IsNull(_transactionField.GetValue(repo));
            }
            finally
            {
                creator.SafeDisposeConnection(conn);
            }
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
