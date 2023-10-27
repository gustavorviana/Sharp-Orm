using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class QueryConstructorTest : BaseTest
    {
        private static readonly QueryInfo info = new(new MysqlQueryConfig(), new("Example"));

        [TestMethod]
        public void AddRaw()
        {
            AssertQuery(GetQuery().Add("SELECT * FROM `Example`"), "SELECT * FROM `Example`");
        }

        [TestMethod]
        public void AddExpression()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col = ?", 1)), "Col = 1");
        }

        [TestMethod]
        public void AddExpressionArgs()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?,?,?,?)", 1, "2", false, true)), "Col IN (1,?,0,1)", "2");
        }

        [TestMethod]
        public void AddExpressionColArgs()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col = ?", new Column("Col2"))), "Col = `Col2`");
        }

        [TestMethod]
        public void AddExpressionAsArgs()
        {
            DateTime now = DateTime.Now;
            var exp1 = new SqlExpression("?,?,?,?,?", true, false, now, 150.1, "Text");
            var exp2 = new SqlExpression("?,?", 2, exp1);
            var exp3 = new SqlExpression("1,?", exp2);
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?)", exp3)), "Col IN (1,2,1,0,?,150.1,?)", now, "Text");
        }

        [TestMethod]
        public void AddExpressionObjArgs()
        {
            DateTime now = DateTime.Now;
            TimeSpan time = now.TimeOfDay;
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?,?,?,?,?,?,?)", 1, "2", true, false, 1.1, now, time)), "Col IN (1,?,1,0,1.1,?,?)", "2", now, time);
        }

        private static void AssertQuery(QueryConstructor actual, string expectedSql, params object[] expectedArgs)
        {
            var exp = actual.ToExpression(null);
            Assert.AreEqual(expectedSql, exp.ToString());
            CollectionAssert.AreEqual(exp.Parameters, expectedArgs);
        }

        private static QueryConstructor GetQuery()
        {
            return new QueryConstructor(info);
        }
    }
}
