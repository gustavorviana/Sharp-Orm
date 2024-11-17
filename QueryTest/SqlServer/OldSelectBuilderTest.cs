using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class OldSelectBuilderTest(ITestOutputHelper output) : DbGrammarTestBase(output, new SqlServerQueryConfig { UseOldPagination = true })
    {

        [Fact]
        public void SelectOffset()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("Id").Offset = 1;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] ASC) AS [grammar_rownum], * FROM [TestTable]) [TestTable] WHERE [grammar_rownum] > 1", sqlExpression);
        }

        [Fact]
        public void SelectOffsetWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] ASC) AS [grammar_rownum], * FROM [TestTable] WHERE [Id] = 1) [TestTable] WHERE [grammar_rownum] > 1", sqlExpression);
        }


        [Fact]
        public void SelectAndPaginate()
        {
            using var query = new Query(TestTableUtils.TABLE + " table");
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] DESC) AS [grammar_rownum], [Id], [Name] FROM [TestTable] [table]) [table] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [Fact]
        public void SelectWhereAndPaginate()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            query.Where("id", 1);

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] DESC) AS [grammar_rownum], [Id], [Name] FROM [TestTable] WHERE [id] = 1) [TestTable] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [Fact]
        public void SelectGroupByPaginate()
        {
            using var query = new Query("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]");
            query.Offset = 1;
            query.Limit = 10;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] ASC) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [Fact]
        public void SelectWhereGroupByPaginate()
        {
            using var query = new Query("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Offset = 1;
            query.Limit = 10;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] ASC) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [Fact]
        public void SelectGroupByPaginateInnerJoin()
        {
            using var query = new Query("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Join("User", "User.Id", "=", "Customer.UserId");
            query.Offset = 1;
            query.Limit = 10;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] ASC) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] INNER JOIN [User] ON [User].[Id] = [Customer].[UserId] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

    }
}
