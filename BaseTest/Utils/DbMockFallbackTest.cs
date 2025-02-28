using BaseTest.Fixtures;
using BaseTest.Mock;
using SharpOrm;
using SharpOrm.Builder;
using System.Data.Common;
using System.Text;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public class DbMockFallbackTest : DbMockTest
    {
        public DbMockFallbackTest(ITestOutputHelper? output, DbFixtureBase connection) : base(output, connection)
        {
        }

        public DbMockFallbackTest(ITestOutputHelper? output) : base(output)
        {

        }

        public DbMockFallbackTest() : base()
        {

        }

        protected QueryFallback RegisterFallback(params Cell[] cells)
        {
            return new QueryFallback(Connection, sql => new MockDataReader(cells));
        }

        protected QueryFallback RegisterFallback(Func<MockCommand, MockDataReader> fallback)
        {
            return new QueryFallback(Connection, fallback);
        }

        protected class QueryFallback : IDisposable, ISqlExpressible
        {
            private readonly StringBuilder builder = new();

            private readonly Func<MockCommand, MockDataReader> fallback;
            private readonly MockConnection connection;
            private readonly List<DbParameter> parameters = [];

            internal QueryFallback(MockConnection connection, Func<MockCommand, MockDataReader>? fallback)
            {
                this.connection = connection;

                connection.OnQueryFallback += (this.fallback = RegisterFallback(fallback));
            }

            private Func<MockCommand, MockDataReader> RegisterFallback(Func<MockCommand, MockDataReader>? fallback)
            {
                if (fallback == null) return cmd =>
                {
                    parameters.AddRange(cmd.Parameters.OfType<DbParameter>());
                    builder.AppendLine(cmd.CommandText);
                    return new MockDataReader();
                };

                return cmd =>
                {
                    parameters.AddRange(cmd.Parameters.OfType<DbParameter>());
                    builder.AppendLine(cmd.CommandText);
                    return fallback(cmd);
                };
            }

            public void Clear()
            {
                builder.Clear();
                parameters.Clear();
            }

            public void Dispose()
            {
#pragma warning disable CS8601 // Possível atribuição de referência nula.
                connection.OnQueryFallback -= fallback;
#pragma warning restore CS8601 // Possível atribuição de referência nula.
            }

            public DbParameter[] GetParameters()
            {
                return [.. parameters];
            }

            public override string ToString()
            {
                return builder.ToString().TrimEnd();
            }

            public SqlExpression ToExpression(IReadonlyQueryInfo info)
            {
                return new(ToString());
            }
        }
    }
}
