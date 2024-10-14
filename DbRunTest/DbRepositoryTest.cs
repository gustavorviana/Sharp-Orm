using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm.Errors;
using System.Data.Common;
using System.Data.SqlClient;

namespace UnityTest
{
    public class DbRepositoryTest() : DbTestBase(new DbFixture<MockConnection>())
    {
        [Fact]
        public void MultipleCommit()
        {
            using var repo = new TestRepository(this.Creator);

            repo.BeginTransaction();
            Assert.Throws<DatabaseException>(repo.BeginTransaction);

            repo.CommitTransaction();
            Assert.Throws<DatabaseException>(repo.CommitTransaction);
        }

        [Fact]
        public void MultipleRollback()
        {
            using var repo = new TestRepository(this.Creator);

            repo.BeginTransaction();
            Assert.Throws<DatabaseException>(repo.BeginTransaction);

            repo.RollbackTransaction();
            Assert.Throws<DatabaseException>(repo.RollbackTransaction);
        }

        [Fact]
        public void MultipleCallBack()
        {
            using var repo = new TestRepository(this.Creator);
            repo.RunTransaction(() =>
            {
                var transaction = repo.GetTransaction();
                Assert.NotNull(transaction);

                repo.RunTransaction(() => Assert.Equal(transaction, repo.GetTransaction()));
                Assert.NotNull(repo.GetTransaction());
            });

            Assert.Null(repo.GetTransaction());
        }

        [Fact]
        public void ExternalTransaction()
        {
            var conn = this.Creator.GetConnection();
            try
            {
                using var transaction = conn.BeginTransaction();
                using var repo = new TestRepository(this.Creator);
                repo.SetTransaction(transaction);

                Assert.Equal(transaction, repo.GetTransaction());
                repo.RunTransaction(() => Assert.Equal(transaction, repo.GetTransaction()));

                Assert.Equal(transaction, repo.GetTransaction());

                repo.SetTransaction((DbTransaction)null!);
                Assert.Null(repo.GetTransaction());
            }
            finally
            {
                this.Creator.SafeDisposeConnection(conn);
            }
        }

        [Fact]
        public void CreateCommandWithTransaction()
        {
            using var repo = new TestRepository(this.Creator);
            repo.RunTransaction(() =>
            {
                using var cmd1 = repo.CreateCommand("");
                Assert.NotNull(cmd1.Transaction);

                using var cmd2 = repo.CreateCommand("", []);
                Assert.NotNull(cmd2.Transaction);
            });
        }

        [Fact]
        public void QueryTransaction()
        {
            using var repo = new TestRepository(this.Creator);
            var query = repo.GetQuery();

            Assert.Null(query.Manager.Transaction);

            repo.BeginTransaction();
            query = repo.GetQuery();
            Assert.NotNull(query.Manager.Transaction);

            repo.CommitTransaction();
            query = repo.GetQuery();
            Assert.Null(query.Manager.Transaction);
        }

        [Fact]
        public void ConnectionDisposeCleanTest()
        {
            using var repo = new TestRepository(this.Creator);

            for (int i = 0; i < 5; i++)
                repo.GetConnection()?.Dispose();

            Assert.Empty(repo._connections);
        }
    }
}
