using SharpOrm;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace BaseTest.Mock
{
    public class MockConnection : DbConnection
    {
        /// <summary>
        /// Return true to signal handled
        /// </summary>
        public Func<MockCommand, MockDataReader?> OnQueryFallback = null!;

        public readonly Dictionary<string, Func<MockDataReader>> QueryReaders = [];
        private ConnectionState state = ConnectionState.Closed;
        [AllowNull]
        public Func<string, int> OnExecuteNonQuery;
        [AllowNull]
        private string database;

        public bool ThrowIfNoQuery { get; set; }

        [AllowNull]
        public override string ConnectionString { get; set; }

        public override string Database => database;

        public override string DataSource => "Src";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => state;

        public override void ChangeDatabase(string databaseName)
        {
            database = databaseName;
        }

        public override void Close()
        {
            state = ConnectionState.Closed;
        }

        public override void Open()
        {
            state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new MockTransaction(this);
        }

        public void Reset()
        {
            OnQueryFallback = null!;
        }

        public MockConnection Register(string query, params Cell[] cells)
        {
            QueryReaders[query] = () => new MockDataReader(cells);
            return this;
        }

        protected override DbCommand CreateDbCommand()
        {
            var cmd = new MockCommand
            {
                Connection = this,
                OnExecuteNonQuery = OnExecuteNonQuery,
                OnGetReader = cmd =>
                {
                    var now = DateTime.Now;
                    try
                    {
                        if (QueryReaders.TryGetValue(cmd.CommandText, out var readerCall))
                            return readerCall().SetCommand(cmd);
                    }
                    finally
                    {
                        System.Diagnostics.Debug.WriteLine("Load reader delay " + (DateTime.Now - now).TotalSeconds);
                    }

                    if (OnFallback(cmd) is MockDataReader reader)
                        return reader;

                    if (ThrowIfNoQuery)
                        throw new Exception("Required query not found: " + cmd.CommandText);

                    System.Diagnostics.Debug.WriteLine(cmd.CommandText);
                    return null!;
                }
            };

            return cmd;
        }

        internal MockDataReader? OnFallback(MockCommand cmd)
        {
            return OnQueryFallback != null ? OnQueryFallback(cmd) : null;
        }
    }
}
