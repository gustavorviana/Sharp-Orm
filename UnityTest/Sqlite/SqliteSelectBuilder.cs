using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.Sqlite
{
    [TestClass]
    public class SqliteSelectBuilder : SqliteTableTest
    {

        [TestMethod]
        public void CountAllDistinct()
        {
            using var query = new Query(TABLE, Creator);
            query.Distinct = true;
            var g = new SqliteGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM (SELECT DISTINCT * FROM \"TestTable\") \"count\"", sqlExpression);
        }

        [TestMethod]
        public void CountAllOfTableDistinct()
        {
            using var query = new Query(TABLE + " t", Creator);
            query.Select("t.*").Distinct = true;
            var g = new SqliteGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM (SELECT DISTINCT \"t\".* FROM \"TestTable\" \"t\") \"count\"", sqlExpression);
        }

        [TestMethod]
        public void PaginateDistinctColumn()
        {
            using var query = new Query<TestTable>(Creator);
            query.OrderBy(NAME);
            query.Distinct = true;
            query.Select(NAME);

            var g = new SqliteGrammar(query);
            var sqlExpression = g.Count((Column)"COUNT(DISTINCT name)");
            TestAssert.AreDecoded("SELECT COUNT(DISTINCT name) FROM \"TestTable\"", sqlExpression);
        }
    }
}
