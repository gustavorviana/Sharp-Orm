using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
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
            return new Query(Creator, table);
        }

        protected static Query<T> Query<T>(string alias) where T : class, new()
        {
            return new Query<T>(Creator, alias);
        }

        [TestInitialize]
        public void InitializeTest()
        {
            Connection.QueryReaders.Clear();
        }

        public static MockDataReader GetReader<T>(int qtd, Func<int, T> createCallback, bool loadForeign)
        {
            T[] values = new T[qtd];

            for (int i = 0; i < qtd; i++)
                values[i] = createCallback(i);

            return GetReader(values, loadForeign);
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

        public static MockDataReader GetReader<T>(T[] objs, bool loadForeign)
        {
            Row[] rows = new Row[objs.Length];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = TableReaderBase.ToRow(objs[i], typeof(T), true, loadForeign);

            return GetReader(rows);
        }

        public static MockDataReader GetReader(Row[] rows)
        {
            return new MockDataReader(rows);
        }
    }
}
