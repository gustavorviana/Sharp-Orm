using SharpOrm;
using SharpOrm.Connection;
using System.Data.Common;

namespace BaseTest.Models
{
    public class TestRepository(ConnectionCreator creator) : DbRepository(false)
    {
        protected override ConnectionCreator Creator { get; } = creator;

        public DbTransaction? GetTransaction()
        {
            return Transaction?.Transaction;
        }

        public DbConnection? GetConnection()
        {
            return GetManager()?.Connection;
        }

        public Query GetQuery()
        {
            return Query("Test");
        }
    }
}
