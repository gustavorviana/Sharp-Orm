using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryBuilderTest(ITestOutputHelper? output) : DbMockTest(output)
    {
        private static readonly QueryInfo info = new(new MysqlQueryConfig(), new("Example"));

        [Fact]
        public void AddRaw()
        {
            AssertQuery(GetQuery().Add("SELECT * FROM [Example]"), "SELECT * FROM [Example]");
        }

        [Fact]
        public void AddExpression()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col = ?", 1)), "Col = 1");
        }

        [Fact]
        public void AddExpressionArgs()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?,?,?,?)", 1, "2", false, true)), "Col IN (1,?,0,1)", "2");
        }

        [Fact]
        public void AddExpressionColArgs()
        {
            AssertQuery(GetQuery().Add(new SqlExpression("Col = ?", new Column("Col2"))), "Col = `Col2`");
        }

        [Fact]
        public void AddExpressionAsArgs()
        {
            DateTime now = DateTime.Now;
            var exp1 = new SqlExpression("?,?,?,?,?", true, false, now, 150.1, "Text");
            var exp2 = new SqlExpression("?,?", 2, exp1);
            var exp3 = new SqlExpression("1,?", exp2);
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?)", exp3)), "Col IN (1,2,1,0,?,150.1,?)", now, "Text");
        }

        [Fact]
        public void AddExpressionObjArgs()
        {
            DateTime now = DateTime.Now;
            TimeSpan time = now.TimeOfDay;
            AssertQuery(GetQuery().Add(new SqlExpression("Col IN (?,?,?,?,?,?,?)", 1, "2", true, false, 1.1, now, time)), "Col IN (1,?,1,0,1.1,?,?)", "2", now, time);
        }

        private static void AssertQuery(QueryBuilder actual, string expectedSql, params object[] expectedArgs)
        {
            var exp = actual.ToExpression(null);
            Assert.Equal(expectedSql, exp.ToString());
            Assert.Equal(exp.Parameters, expectedArgs);
        }

        private static QueryBuilder GetQuery()
        {
            return new QueryBuilder(info);
        }
    }
}
