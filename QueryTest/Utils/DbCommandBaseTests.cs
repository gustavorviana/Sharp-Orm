using NSubstitute;
using System.Data.Common;

namespace QueryTest.Utils
{
    public abstract class DbCommandBaseTests
    {
        protected readonly DbCommand Command;
        protected readonly DbDataReader Reader;
        protected readonly DbParameterCollection Parameters;
        protected readonly DbConnection Connection;

        public DbCommandBaseTests()
        {
            Reader = Substitute.For<DbDataReader>();
            Parameters = Substitute.For<DbParameterCollection>();

            Command = Substitute.For<DbCommand>();
            Command.Parameters.Returns(Parameters);
            Command.ExecuteReader().Returns(Reader);

            Connection = Substitute.For<DbConnection>();
            Connection.CreateCommand().Returns(Command);

            Command.Connection.Returns(Connection);
            Command.ExecuteReader().Returns(Reader);
        }
    }
}
