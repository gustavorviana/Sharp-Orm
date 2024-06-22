using System.Data;
using System.Data.Common;

namespace UnityTest.Utils.Mock
{
    internal class MockTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel { get; }

        private readonly DbConnection connection;
        protected override DbConnection DbConnection => connection;

        public MockTransaction(MockConnection connection)
        {
            this.connection = connection;
        }

        public override void Commit()
        {

        }

        public override void Rollback()
        {

        }
    }
}
