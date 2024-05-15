using SharpOrm;
using SharpOrm.Connection;
using System.Data.Common;

namespace UnityTest.Models
{
    internal class TestRepository : DbRepository
    {
        protected override ConnectionCreator Creator { get; }
        public TestRepository(ConnectionCreator creator) : base(false)
        {
            this.Creator = creator;
        }

        public DbTransaction GetTransaction()
        {
            return this.Transaction?.Transaction;
        }

        public DbConnection GetConnection()
        {
#pragma warning disable CS0618 // O tipo ou membro é obsoleto
            return base.GetConnection();
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
        }

        public Query GetQuery()
        {
            return this.Query("Test");
        }
    }
}
