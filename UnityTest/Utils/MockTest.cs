using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using UnityTest.Utils.Mock;

namespace UnityTest.Utils
{
    public class MockTest : BaseTest
    {
        protected static readonly MultipleConnectionCreator<MockConnection> Creator = new(new MysqlQueryConfig(false), "");
        protected static readonly MysqlQueryConfig Config = new(false);
        protected static readonly MockConnection Connection = new();

        protected static Query Query(string table)
        {
            return new Query(table, Creator);
        }

        protected static Query<T> Query<T>(string alias) where T : class, new()
        {
            return new Query<T>(alias, Creator);
        }

        [TestInitialize]
        public void InitializeTest()
        {
            Connection.QueryReaders.Clear();
        }

        public static MockDataReader GetReader(Func<int, Cell[]> createCallback, int qtd)
        {
            return new MockDataReader(i => new Row(createCallback(i)), qtd);
        }

        public static MockDataReader GetReader<T>(int qtd, Func<int, T> createCallback, bool loadForeign)
        {
            Type type = typeof(T);
            return new MockDataReader(i => Row.Parse(createCallback(i), type, true, loadForeign), qtd);
        }

        protected static MockConnection GetNonQueryCommand(string expected)
        {
            var conn = new MockConnection();
            conn.OnExecuteNonQuery += (string query) =>
            {
                Assert.AreEqual(expected, query);
                return -1;
            };

            return conn;
        }
    }
}
