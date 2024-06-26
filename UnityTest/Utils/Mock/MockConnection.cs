﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace UnityTest.Utils.Mock
{
    public class MockConnection : DbConnection
    {
        public readonly Dictionary<string, Func<MockDataReader>> QueryReaders = new();
        private ConnectionState state = ConnectionState.Closed;
        public Func<string, int> OnExecuteNonQuery;
        private string database;

        public bool ThrowIfNoQuery { get; set; }

        public override string ConnectionString { get; set; }

        public override string Database => database;

        public override string DataSource => "Src";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => state;

        public override void ChangeDatabase(string databaseName)
        {
            this.database = databaseName;
        }

        public override void Close()
        {
            this.state = ConnectionState.Closed;
        }

        public override void Open()
        {
            this.state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new MockTransaction(this);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new MockCommand
            {
                Connection = this,
                OnExecuteNonQuery = OnExecuteNonQuery,
                OnGetReader = cmd =>
                {
                    var now = DateTime.Now;
                    try
                    {
                        if (this.QueryReaders.TryGetValue(cmd.CommandText, out var reader))
                            return reader();
                    }
                    finally
                    {
                        System.Diagnostics.Debug.WriteLine("Load reader delay " + (DateTime.Now - now).TotalSeconds);
                    }

                    if (ThrowIfNoQuery)
                        throw new Exception("Required query not found: " + cmd.CommandText);

                    System.Diagnostics.Debug.WriteLine(cmd.CommandText);
                    return null;
                }
            };
        }
    }
}
