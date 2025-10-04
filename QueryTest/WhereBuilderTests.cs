using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
namespace QueryTest
{
    public class WhereBuilderTests : DbMockFallbackTest
    {
        public WhereBuilderTests()
        {
            SetMockConnectionVersion();
        }

        [Fact]
        public void WhereBuilder_SimpleCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Id, "=", 1));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Id] =", sql);
        }

        [Fact]
        public void WhereBuilder_MultipleConditions()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Id, ">", 1)
                .Where(t => t.Name, "=", "Test"));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Id] >", sql);
            Assert.Contains("AND", sql);
            Assert.Contains("[Name] =", sql);
        }

        [Fact]
        public void WhereBuilder_OrCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Id, "=", 1)
                .OrWhere(t => t.Id, "=", 2));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Id] =", sql);
            Assert.Contains("OR", sql);
        }

        [Fact]
        public void WhereBuilder_WithCallback()
        {
            using var query = new Query<TestTable>();
            query.Where(w => w
                .Where(t => t.Id, ">", 10)
                .Where(t => t.Name, "LIKE", "%test%"));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Id] >", sql);
            Assert.Contains("[Name] LIKE", sql);
        }

        [Fact]
        public void WhereBuilder_CombineMultipleBuilders()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Id, ">", 1))
                 .Where(x => x.Where(t => t.Name, "=", "Test"));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Id] >", sql);
            Assert.Contains("[Name] =", sql);
        }

        [Fact]
        public void WhereBuilder_OrWhereWithBuilder()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Number, ">", 100))
                 .OrWhere(x => x.Where(t => t.Name, "=", "Admin"));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Number] >", sql);
            Assert.Contains("OR", sql);
            Assert.Contains("[Name] =", sql);
        }

        [Fact]
        public void WhereBuilder_OrWhereWithCallback()
        {
            using var query = new Query<TestTable>();
            query.Where(w => w.Where(t => t.Number, ">", 100))
                 .OrWhere(w => w.Where(t => t.Name, "=", "Admin"));
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
            Assert.Contains("[Number] >", sql);
            Assert.Contains("OR", sql);
            Assert.Contains("[Name] =", sql);
        }
    }
}
