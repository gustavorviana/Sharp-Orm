using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
namespace QueryTest
{
    public class WhereExtensionsTests : DbMockFallbackTest
    {
        public WhereExtensionsTests()
        {
            SetMockConnectionVersion();
        }
        #region Where Extensions
        [Fact]
        public void Where_WithTwoParameters_UsesEqualOperation()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Name, "Test");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] =", sql);
        }
        [Fact]
        public void WhereNull_GeneratesIsNullCondition()
        {
            using var query = new Query<TestTable>();
            query.WhereNull(t => t.Name);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] IS NULL", sql);
        }
        [Fact]
        public void WhereNotNull_GeneratesIsNotNullCondition()
        {
            using var query = new Query<TestTable>();
            query.WhereNotNull(t => t.Name);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] IS NOT NULL", sql);
        }
        [Fact]
        public void WhereIn_WithTypedExpression()
        {
            using var query = new Query<TestTable>();
            query.WhereIn(t => t.Id, 1, 2, 3);
            QueryAssert.Equal("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Id] IN (1, 2, 3)", query.Grammar().Select());
        }
        [Fact]
        public void WhereNotIn_WithTypedExpression()
        {
            using var query = new Query<TestTable>();
            query.WhereNotIn(t => t.Id, 1, 2, 3);
            QueryAssert.Equal("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Id] NOT IN (1, 2, 3)", query.Grammar().Select());
        }
        [Fact]
        public void Where_WithLambdaExpression()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 10).Where(t => t.Name, "!=", null);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("WHERE", sql);
        }
        #endregion
        #region OrWhere Extensions
        [Fact]
        public void OrWhere_WithTwoParameters_UsesEqualOperation()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .OrWhere(t => t.Name, "Test");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] =", sql);
            Assert.Contains("OR", sql);
        }
        [Fact]
        public void OrWhere_WithLambdaExpression()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 10)
                 .OrWhere(t => t.Name, "=", "Test");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("OR", sql);
        }
        [Fact]
        public void OrWhereNull_GeneratesOrIsNullCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .OrWhereNull("Name");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("OR", sql);
            Assert.Contains("IS NULL", sql);
        }
        [Fact]
        public void OrWhereNotNull_GeneratesOrIsNotNullCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .OrWhereNotNull("Name");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("OR", sql);
            Assert.Contains("IS NOT NULL", sql);
        }
        #endregion
        #region Chaining Tests
        [Fact]
        public void Extensions_CanChainMultipleCalls()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .Where(t => t.Number, 100)
                 .WhereNotNull(t => t.Name)
                 .OrWhere(t => t.Nick, "Admin");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Id] >", sql);
            Assert.Contains("[Number] =", sql);
            Assert.Contains("[Name] IS NOT NULL", sql);
            Assert.Contains("[Nick] =", sql);
            Assert.Contains("OR", sql);
        }
        [Fact]
        public void WhereBuilder_CanUseExtensions()
        {
            using var query = new Query<TestTable>();
            var builder = new WhereBuilder<TestTable>(query.Info.Config, query.Info.TableName);
            builder.Where(t => t.Id, 1)
                   .WhereNotNull("Name");
            query.Where(builder);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Id] =", sql);
            Assert.Contains("IS NOT NULL", sql);
        }
        #endregion
        #region QueryBase Extensions
        [Fact]
        public void WhereIn_OnQueryBase()
        {
            using var query = new Query("TestTable");
            query.WhereIn("Id", 1, 2, 3);
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Id] IN (1, 2, 3)", query.Grammar().Select());
        }
        [Fact]
        public void WhereNotIn_OnQueryBase()
        {
            using var query = new Query("TestTable");
            query.WhereNotIn("Id", 1, 2, 3);
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Id] NOT IN (1, 2, 3)", query.Grammar().Select());
        }
        [Fact]
        public void WhereNull_OnQueryBase()
        {
            using var query = new Query("TestTable");
            query.WhereNull("Name");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] IS NULL", sql);
        }
        [Fact]
        public void WhereNotNull_OnQueryBase()
        {
            using var query = new Query("TestTable");
            query.WhereNotNull("Name");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] IS NOT NULL", sql);
        }
        #endregion
        #region WhereLike Tests
        [Fact]
        public void WhereLike_WithStringValue_ShouldGenerateLikeCondition()
        {
            using var query = new Query("TestTable");
            query.WhereLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Name] LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereLike_WithTypedExpression_ShouldGenerateLikeCondition()
        {
            using var query = new Query<TestTable>();
            query.WhereLike(t => t.Name, "%Test%");
            QueryAssert.EqualDecoded("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Name] LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereNotLike_WithStringValue_ShouldGenerateNotLikeCondition()
        {
            using var query = new Query("TestTable");
            query.WhereNotLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Name] NOT LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereNotLike_WithTypedExpression_ShouldGenerateNotLikeCondition()
        {
            using var query = new Query<TestTable>();
            query.WhereNotLike(t => t.Name, "%Test%");
            QueryAssert.EqualDecoded("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Name] NOT LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void OrWhereLike_ShouldGenerateOrLikeCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .OrWhereLike(t => t.Name, "%Test%");
            QueryAssert.EqualDecoded("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Id] > 1 OR [Name] LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void OrWhereNotLike_ShouldGenerateOrNotLikeCondition()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .OrWhereNotLike(t => t.Name, "%Test%");
            QueryAssert.EqualDecoded("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Id] > 1 OR [Name] NOT LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereLike_CanChainWithOtherConditions()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, ">", 1)
                 .WhereLike(t => t.Name, "%Test%")
                 .WhereNotNull(t => t.Nick);
            QueryAssert.EqualDecoded("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable] WHERE [Id] > 1 AND [Name] LIKE @p1 AND [Nick] IS NOT NULL", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereLike_OnQueryBase_ShouldWork()
        {
            using var query = new Query("TestTable");
            query.WhereLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Name] LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void WhereNotLike_OnQueryBase_ShouldWork()
        {
            using var query = new Query("TestTable");
            query.WhereNotLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Name] NOT LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void OrWhereLike_OnQueryBase_ShouldWork()
        {
            using var query = new Query("TestTable");
            query.Where("Id", ">", 1)
                 .OrWhereLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Id] > 1 OR [Name] LIKE @p1", ["%Test%"], query.Grammar().Select());
        }

        [Fact]
        public void OrWhereNotLike_OnQueryBase_ShouldWork()
        {
            using var query = new Query("TestTable");
            query.Where("Id", ">", 1)
                 .OrWhereNotLike("Name", "%Test%");
            QueryAssert.EqualDecoded("SELECT * FROM [TestTable] WHERE [Id] > 1 OR [Name] NOT LIKE @p1", ["%Test%"], query.Grammar().Select());
        }
        #endregion
    }
}
