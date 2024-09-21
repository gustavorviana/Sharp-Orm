using SharpOrm.Fb;
using SharpOrm.Connection;
using FirebirdSql.Data.FirebirdClient;

namespace UnityTest.Utils
{
    public abstract class FbTest : DbTest<FbConnection>
    {
        public FbTest() : base(new FbQueryConfig() { LoadForeign = true }, ConnectionStr.Fb)
        {

        }

        protected static ConnectionCreator GetCreator()
        {
            return new SingleConnectionCreator<FbConnection>(new FbQueryConfig(), ConnectionStr.Fb);
        }
    }
}
