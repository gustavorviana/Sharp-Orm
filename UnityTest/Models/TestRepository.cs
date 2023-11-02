using SharpOrm;
using SharpOrm.Connection;

namespace UnityTest.Models
{
    internal class TestRepository : DbRepository
    {
        protected override ConnectionCreator Creator { get; }
        public TestRepository(ConnectionCreator creator)
        {
            this.Creator = creator;
        }

        public Query GetQuery()
        {
            return this.Query("Test");
        }
    }
}
