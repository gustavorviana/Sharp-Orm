using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace BaseTest.Mock
{
    public class MockCommand : DbCommand
    {
        private bool _cancelled = false;
        public bool Cancelled => _cancelled;
        public event EventHandler? OnCancel;

        [AllowNull]
        public Func<string, int> OnExecuteNonQuery;
        [AllowNull]
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        [AllowNull]
        protected override DbConnection DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new MockParamsCollection();

        [AllowNull]
        protected override DbTransaction DbTransaction { get; set; }

        [AllowNull]
        public Func<MockCommand, MockDataReader> OnGetReader;

        public override void Cancel()
        {
            _cancelled = true;
            OnCancel?.Invoke(this, EventArgs.Empty);
        }

        public override int ExecuteNonQuery()
        {
            if (OnExecuteNonQuery == null)
            {
                ((MockConnection)DbConnection).OnFallback(this);
                return -1;
            }

            return OnExecuteNonQuery?.Invoke(CommandText) ?? -1;
        }

        public override object ExecuteScalar()
        {
            var reader = GetReader();
            if (reader.Size == 0)
                return DBNull.Value;

            return reader[0];
        }

        public override void Prepare()
        {

        }

        protected override DbParameter CreateDbParameter()
        {
            return new SqlParameter();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return GetReader();
        }

        private MockDataReader GetReader()
        {
            var reader = OnGetReader?.Invoke(this);
            if (reader == null)
                return new MockDataReader(i => null!, 0);

            return reader;
        }
    }
}
