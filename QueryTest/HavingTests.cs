using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using Xunit.Abstractions;

namespace QueryTest
{
    public class HavingTests(ITestOutputHelper? output) : DbMockTest(output)
    {
        [Fact]
        public void Having_WithSqlExpression_ShouldAddToQuery()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5", sql);
        }

        [Fact]
        public void Having_WithCallback_ShouldAddToQuery()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(w => w.Where(new SqlExpression("COUNT(*) > 5")));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5", sql);
        }

        [Fact]
        public void Having_Multiple_ShouldCombineWithAND()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));
            q.Having(new SqlExpression("SUM(Amount) > 1000"));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5 AND SUM(Amount) > 1000", sql);
        }

        [Fact]
        public void Having_MultipleWithCallback_ShouldCombineWithAND()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(w => w.Where(new SqlExpression("COUNT(*) > 5")));
            q.Having(w => w.Where(new SqlExpression("SUM(Amount) > 1000")));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5 AND SUM(Amount) > 1000", sql);
        }

        [Fact]
        public void Having_MixedTypes_ShouldCombineWithAND()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));
            q.Having(w => w.Where(new SqlExpression("SUM(Amount) > 1000")));
            q.Having(new SqlExpression("AVG(Price) < 100"));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5 AND SUM(Amount) > 1000 AND AVG(Price) < 100", sql);
        }

        [Fact]
        public void Having_TypedQuery_WithSqlExpression_ShouldAddToQuery()
        {
            using var q = new Query<TestClass>(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5", sql);
        }

        [Fact]
        public void Having_TypedQuery_WithCallback_ShouldAddToQuery()
        {
            using var q = new Query<TestClass>(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(w => w.Where(new SqlExpression("COUNT(*) > 5")));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5", sql);
        }

        [Fact]
        public void Having_TypedQuery_Multiple_ShouldCombineWithAND()
        {
            using var q = new Query<TestClass>(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));
            q.Having(w => w.Where(new SqlExpression("SUM(Amount) > 1000")));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5 AND SUM(Amount) > 1000", sql);
        }

        [Fact]
        public void Having_WithComplexCallback_ShouldWork()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");
            q.Having(w =>
            {
                w.Where(new SqlExpression("COUNT(*) > 5"));
                w.OrWhere(new SqlExpression("SUM(Amount) > 10000"));
            });

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("HAVING COUNT(*) > 5 OR SUM(Amount) > 10000", sql);
        }

        [Fact]
        public void Having_EmptyQuery_ShouldNotThrowException()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.GroupBy("Category");

            var exception = Record.Exception(() => q.Having(new SqlExpression("COUNT(*) > 5")));
            Assert.Null(exception);
        }

        [Fact]
        public void Having_WithWhereClause_ShouldBothExist()
        {
            using var q = new Query(TestTableUtils.TABLE);
            q.Where("Active", true);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            var sql = q.Grammar().SelectSqlOnly();
            Assert.Contains("WHERE", sql);
            Assert.Contains("HAVING COUNT(*) > 5", sql);
        }

        [Fact]
        public void Having_ChainedCalls_ShouldReturnSameQueryInstance()
        {
            using var q = new Query(TestTableUtils.TABLE);
            var result = q.GroupBy("Category")
                          .Having(new SqlExpression("COUNT(*) > 5"))
                          .Having(new SqlExpression("SUM(Amount) > 1000"));

            Assert.Same(q, result);
        }

        [Fact]
        public void Having_TypedQuery_ChainedCalls_ShouldReturnCorrectType()
        {
            using var q = new Query<TestClass>(TestTableUtils.TABLE);
            var result = q.GroupBy("Category")
                          .Having(new SqlExpression("COUNT(*) > 5"));

            Assert.IsType<Query<TestClass>>(result);
        }
    }
}
