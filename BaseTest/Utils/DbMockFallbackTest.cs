using BaseTest.Mock;
using SharpOrm;
using System.Text;

namespace BaseTest.Utils
{
    public class DbMockFallbackTest : DbMockTest
    {
        protected QueryFallback RegisterFallback(params Cell[] cells)
        {
            return new QueryFallback(Connection, sql => new MockDataReader(cells));
        }

        protected QueryFallback RegisterFallback(Func<string, MockDataReader> fallback)
        {
            return new QueryFallback(Connection, fallback);
        }

        protected class QueryFallback : IDisposable
        {
            private readonly StringBuilder builder = new();

            private readonly Func<string, MockDataReader> fallback;
            private readonly MockConnection connection;

            internal QueryFallback(MockConnection connection, Func<string, MockDataReader>? fallback)
            {
                this.connection = connection;

                connection.OnQueryFallback += (this.fallback = this.RegisterFallback(fallback));
            }

            private Func<string, MockDataReader> RegisterFallback(Func<string, MockDataReader>? fallback)
            {
                if (fallback == null) return sql =>
                {
                    this.builder.AppendLine(sql);
                    return new MockDataReader();
                };

                return sql =>
                {
                    this.builder.AppendLine(sql);
                    return fallback(sql);
                };
            }

            public void Clear()
            {
                this.builder.Clear();
            }

            public void Dispose()
            {
#pragma warning disable CS8601 // Possível atribuição de referência nula.
                connection.OnQueryFallback -= fallback;
#pragma warning restore CS8601 // Possível atribuição de referência nula.
            }

            public override string ToString()
            {
                return this.builder.ToString().TrimEnd();
            }
        }
    }
}
