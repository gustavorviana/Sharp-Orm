using BaseTest.Mock;
using SharpOrm;
using System.Data.Common;
using System.Text;

namespace BaseTest.Utils
{
    public class DbMockFallbackTest : DbMockTest
    {
        protected QueryFallback RegisterFallback(params Cell[] cells)
        {
            return new QueryFallback(Connection, sql => new MockDataReader(cells));
        }

        protected QueryFallback RegisterFallback(Func<MockCommand, MockDataReader> fallback)
        {
            return new QueryFallback(Connection, fallback);
        }

        protected class QueryFallback : IDisposable
        {
            private readonly StringBuilder builder = new();

            private readonly Func<MockCommand, MockDataReader> fallback;
            private readonly MockConnection connection;
            private readonly List<DbParameter> parameters = [];

            internal QueryFallback(MockConnection connection, Func<MockCommand, MockDataReader>? fallback)
            {
                this.connection = connection;

                connection.OnQueryFallback += (this.fallback = this.RegisterFallback(fallback));
            }

            private Func<MockCommand, MockDataReader> RegisterFallback(Func<MockCommand, MockDataReader>? fallback)
            {
                if (fallback == null) return cmd =>
                {
                    this.parameters.AddRange(cmd.Parameters.OfType<DbParameter>());
                    this.builder.AppendLine(cmd.CommandText);
                    return new MockDataReader();
                };

                return cmd =>
                {
                    this.parameters.AddRange(cmd.Parameters.OfType<DbParameter>());
                    this.builder.AppendLine(cmd.CommandText);
                    return fallback(cmd);
                };
            }

            public void Clear()
            {
                this.builder.Clear();
                this.parameters.Clear();
            }

            public void Dispose()
            {
#pragma warning disable CS8601 // Possível atribuição de referência nula.
                connection.OnQueryFallback -= fallback;
#pragma warning restore CS8601 // Possível atribuição de referência nula.
            }

            public DbParameter[] GetParameters()
            {
                return [.. this.parameters];
            }

            public override string ToString()
            {
                return this.builder.ToString().TrimEnd();
            }
        }
    }
}
