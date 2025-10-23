using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryExceptionsTest(ITestOutputHelper? output) : DbMockTest(output)
    {
        [Fact]
        public void OnCreateQueryWithNull()
        {
            ConnectionCreator.Default = null;
            Assert.Throws<ArgumentNullException>(() => new Query("SomeTable"));
        }

        [Fact]
        public void OnQueryNullTable()
        {
            Assert.Throws<ArgumentNullException>(() => new Query(null, Creator));
        }

        [Fact]
        public void OnQueryNullConfig()
        {
            using var con = Creator.GetConnection();
            Assert.Throws<ArgumentNullException>(() => new Query("SomeTable", new ConnectionManager(null, con)));
        }

        [Fact]
        public void OnInvalidWhereOperation()
        {
            using var q = new Query(TestTableUtils.TABLE);

            Assert.Throws<ArgumentNullException>(() => q.Where("column", "", "value"));
            Assert.Throws<ArgumentNullException>(() => q.Where("column", null, "value"));
            Assert.Throws<InvalidOperationException>(() => q.Where("column", "None", "value"));
        }

        [Fact]
        public void OnUnsafeUpdateWithoutWhereOrHaving()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);

            // Should throw when updating without WHERE or HAVING
            Assert.Throws<SharpOrm.Errors.UnsafeDbOperation>(() => q.Update(new Cell("Value", 1)));
        }

        [Fact]
        public void OnUnsafeDeleteWithoutWhereOrHaving()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);

            // Should throw when deleting without WHERE or HAVING
            Assert.Throws<SharpOrm.Errors.UnsafeDbOperation>(() => q.Delete());
        }

        [Fact]
        public void OnSafeUpdateWithWhere()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);
            q.Where("Id", 1);

            // Should not throw with WHERE clause
            var exception = Record.Exception(() => q.Update(new Cell("Value", 1)));
            Assert.Null(exception);
        }

        [Fact]
        public void OnSafeUpdateWithHaving()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            // Should not throw with HAVING clause
            var exception = Record.Exception(() => q.Update(new Cell("Value", 1)));
            Assert.Null(exception);
        }

        [Fact]
        public void OnSafeDeleteWithHaving()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            // Should not throw with HAVING clause
            var exception = Record.Exception(() => q.Delete());
            Assert.Null(exception);
        }

        [Fact]
        public void OnSafeUpdateWithWhereAndHaving()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);
            q.Where("Active", true);
            q.GroupBy("Category");
            q.Having(new SqlExpression("COUNT(*) > 5"));

            // Should not throw with both WHERE and HAVING
            var exception = Record.Exception(() => q.Update(new Cell("Value", 1)));
            Assert.Null(exception);
        }

        [Fact]
        public void OnUnsafeUpdateWithOnlyGroupBy()
        {
            using var safeCreator = CloneCreator(true);
            using var q = new Query(TestTableUtils.TABLE, safeCreator);
            q.GroupBy("Category");
            // No WHERE or HAVING

            // Should throw when updating with GROUP BY but without WHERE or HAVING
            Assert.Throws<SharpOrm.Errors.UnsafeDbOperation>(() => q.Update(new Cell("Value", 1)));
        }
    }
}
