using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace UnityTest.Utils.Mock
{
    public class MockCommand : DbCommand
    {
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new MockParamsCollection();

        protected override DbTransaction DbTransaction { get; set; }

        public Func<MockCommand, MockDataReader> OnGetReader;

        public override void Cancel()
        {

        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object ExecuteScalar()
        {
            var reader = this.GetReader(); ;
            if (reader.rows.Length == 0)
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
                return new MockDataReader(Array.Empty<SharpOrm.Row>());

            return reader;
        }
    }
}
