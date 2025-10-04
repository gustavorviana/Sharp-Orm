using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;

namespace QueryTest
{
    public class QueryExtensionsTests : DbMockFallbackTest
    {
        public QueryExtensionsTests()
        {
            SetMockConnectionVersion();
        }

        [Fact]
        public void Skip_SetsOffsetCorrectly()
        {
            using var query = new Query<TestTable>();
            query.Skip(10);

            Assert.Equal(10, query.Offset);
        }

        [Fact]
        public void Take_SetsLimitCorrectly()
        {
            using var query = new Query<TestTable>();
            query.Take(20);

            Assert.Equal(20, query.Limit);
        }

        [Fact]
        public void Page_CalculatesOffsetAndLimitCorrectly()
        {
            using var query = new Query<TestTable>();
            query.Page(3, 10); // Page 3, 10 items per page

            Assert.Equal(20, query.Offset); // (3-1) * 10 = 20
            Assert.Equal(10, query.Limit);
        }

        [Fact]
        public void Page_ThrowsOnInvalidPage()
        {
            using var query = new Query<TestTable>();
            Assert.Throws<ArgumentException>(() => query.Page(0, 10));
            Assert.Throws<ArgumentException>(() => query.Page(-1, 10));
        }

        [Fact]
        public void Page_ThrowsOnInvalidPageSize()
        {
            using var query = new Query<TestTable>();
            Assert.Throws<ArgumentException>(() => query.Page(1, 0));
            Assert.Throws<ArgumentException>(() => query.Page(1, -1));
        }

        [Fact]
        public void Skip_Take_ChainCorrectly()
        {
            using var query = new Query<TestTable>();
            query.Skip(5).Take(10);

            Assert.Equal(5, query.Offset);
            Assert.Equal(10, query.Limit);
        }


        [Fact]
        public void WithTrashed_SetsTrashedToWith()
        {
            using var query = new Query<TestTable>();
            query.WithTrashed();

            Assert.Equal(Trashed.With, query.Trashed);
        }

        [Fact]
        public void WithoutTrashed_SetsTrashedToExcept()
        {
            using var query = new Query<SoftDeleteTestTable>();
            query.WithoutTrashed();

            Assert.Equal(Trashed.Except, query.Trashed);
        }

        [Fact]
        public void OnlyTrashed_SetsTrashedToOnly()
        {
            using var query = new Query<SoftDeleteTestTable>();
            query.OnlyTrashed();

            Assert.Equal(Trashed.Only, query.Trashed);
        }

        [Fact]
        public void SoftDelete_CanChain()
        {
            using var query = new Query<TestTable>();
            var result = query.WithTrashed().Take(10);

            Assert.Equal(Trashed.With, result.Trashed);
            Assert.Equal(10, result.Limit);
        }

        [Fact]
        public void FindById_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            Assert.Throws<SharpOrm.Errors.DatabaseException>(() => query.FindById(1));
        }

        [Fact]
        public void FindById_CreatesNewQuery()
        {
            using var query = new Query<TestClass>();
            query.Where(t => t.MyName, "=", "ShouldNotAffect");

            var result = query.FindById(1);

            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.DoesNotContain("[Id] =", sql);
            Assert.Contains("[MyName] =", sql);
        }

        [Fact]
        public void UpdateById_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            Assert.Throws<SharpOrm.Errors.DatabaseException>(() =>
                query.UpdateById(1, new TestTableNoPk()));
        }

        [Fact]
        public void UpdateById_CreatesNewQuery()
        {
            using var query = new Query<TestClass>();
            query.Where(t => t.MyId, ">", 100);

            var result = query.UpdateById(1, new TestClass { MyName = "Updated" });

            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.DoesNotContain("[Id] =", sql);
            Assert.Contains("[MyId] >", sql);
        }

        [Fact]
        public void DeleteById_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            Assert.Throws<SharpOrm.Errors.DatabaseException>(() => query.DeleteById(1));
        }

        [Fact]
        public void DeleteById_CreatesNewQuery()
        {
            using var query = new Query<TestClass>();
            query.Where(t => t.MyId, ">", 100);

            var result = query.DeleteById(1);

            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.DoesNotContain("[Id] =", sql);
            Assert.Contains("[MyId] >", sql);
        }

        [Fact]
        public async Task FindByIdAsync_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            await Assert.ThrowsAsync<SharpOrm.Errors.DatabaseException>(
                async () => await query.FindByIdAsync(1));
        }

        [Fact]
        public async Task UpdateByIdAsync_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            await Assert.ThrowsAsync<SharpOrm.Errors.DatabaseException>(
                async () => await query.UpdateByIdAsync(1, new TestTableNoPk()));
        }

        [Fact]
        public async Task DeleteByIdAsync_ThrowsOnMissingPrimaryKey()
        {
            using var query = new Query<TestTableNoPk>();
            await Assert.ThrowsAsync<SharpOrm.Errors.DatabaseException>(
                async () => await query.DeleteByIdAsync(1));
        }
    }

    // Helper class for testing queries without primary key
    public class TestTableNoPk
    {
        public string Name { get; set; }
        public bool Active { get; set; }
    }
}
