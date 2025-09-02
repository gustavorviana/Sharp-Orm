using System.Data;
using System.Data.Common;

namespace BaseTest.Mock
{
    internal class MockTransaction(MockConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {

        }

        public override void Rollback()
        {

        }
    }
}
