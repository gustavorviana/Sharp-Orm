using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class QueryTest : MysqlTableTest
    {
        [TestMethod]
        public void OrderBy()
        {
            var q = new Query("table", Creator);
            q.OrderBy(SharpOrm.OrderBy.None, "Col1");
            Assert.AreEqual(0, q.Info.Orders.Length);

            q.OrderBy(SharpOrm.OrderBy.Asc, "Col2");
            Assert.AreEqual(1, q.Info.Orders.Length);

            q.OrderBy(SharpOrm.OrderBy.Desc, "3");
            Assert.AreEqual(1, q.Info.Orders.Length);
        }

        [TestMethod]
        public void Clone()
        {
            var original = new Query("table alias", Creator)
            {
                Limit = 1,
                Offset = 3,
                Distinct = true
            };

            original.Select("Col1", "Col2");
            original.WhereColumn("Col1", "=", "Col2");

            Assert.AreEqual(original.ToString(), original.Clone(true).ToString());
            var clone = original.Clone(false);

            Assert.AreNotEqual(original.ToString(), original.Clone(false).ToString());
            Assert.AreEqual(original.Limit, clone.Limit);
            Assert.AreEqual(original.Offset, clone.Offset);
            Assert.AreEqual(original.Distinct, clone.Distinct);
        }

        [TestMethod]
        public void DefaultTimeoutTest()
        {
            var query = new Query("table", Creator);

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.AreEqual(30, cmd.CommandTimeout);
        }

        [TestMethod]
        public void QueryCustomTimeoutTest()
        {
            var query = new Query("table", Creator)
            {
                CommandTimeout = 120
            };

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.AreEqual(120, cmd.CommandTimeout);
        }

        [TestMethod]
        public void ConfigCustomTimeoutTest()
        {
            using var creator = new MultipleConnectionCreator<MySqlConnection>(new MysqlQueryConfig(false) { CommandTimeout = 120 }, ConnectionStr.Mysql);
            var query = new Query("table", creator);

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.AreEqual(120, cmd.CommandTimeout);
        }
    }
}
