using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace BaseTest.Mock
{
    public class MockCommand : DbCommand
    {
        private bool _cancelled = false;
        public bool Cancelled => this._cancelled;
        public event EventHandler OnCancel;

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
            this._cancelled = true;
            this.OnCancel?.Invoke(this, EventArgs.Empty);
        }

        public override int ExecuteNonQuery()
        {
            return OnExecuteNonQuery?.Invoke(this.CommandText) ?? -1;
        }

        public override object ExecuteScalar()
        {
            var reader = this.GetReader();
            if (reader.Size == 0)
                return DBNull.Value;

            return reader[0];
        }

        public override void Prepare()
        {

        }

        protected override DbParameter CreateDbParameter()
        {
            var param = new SqlParameter();
            this.Parameters.Add(param);
            return param;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return this.GetReader();
        }

        private MockDataReader GetReader()
        {
            var reader = this.OnGetReader?.Invoke(this);
            if (reader == null)
                return new MockDataReader(i => null!, 0);

            return reader;
        }
    }
}
