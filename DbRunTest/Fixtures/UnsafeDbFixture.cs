using SharpOrm.Connection;
using System.Data.Common;

namespace DbRunTest.Fixtures
{
    public class UnsafeDbFixture<Conn> : DbFixture<Conn> where Conn : DbConnection, new()
    {
        protected override ConnectionCreator MakeConnectionCreator()
        {
            var info = GetMap();
            return new MultipleConnectionCreator<Conn>(info.GetConfig(false), info.ConnString);
        }
    }
}
