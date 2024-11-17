using SharpOrm;
using SharpOrm.Connection;
using System.Data.Common;

namespace BaseTest.Models
{
    public class TestRepository : DbRepository
    {
        protected override ConnectionCreator Creator { get; }
        public TestRepository(ConnectionCreator creator) : base(false)
        {
            this.Creator = creator;
        }

        public DbTransaction? GetTransaction()
        {
            return this.Transaction?.Transaction;
        }

        public DbConnection? GetConnection()
        {
            return this.GetManager()?.Connection;
        }

        public Query GetQuery()
        {
            return this.Query("Test");
        }
    }
}
