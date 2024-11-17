using BaseTest.Utils;
using BaseTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class DeleteBuilderTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>, IDeleteBuilderTest
    {
        [Fact]
        public void Delete()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.Equal("DELETE FROM \"TestTable\"", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            QueryAssert.Equal("DELETE FROM \"TestTable\" WHERE \"id\" = 1", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteOrder()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("id");

           Assert.Throws<NotSupportedException>(() => query.Grammar().Delete());
        }

        [Fact]
        public void DeleteLimit()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 5;

            Assert.Throws<NotSupportedException>(() => query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWhereJoin()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            Assert.Throws<NotSupportedException>(() => query.Grammar().Delete());
        }

        [Fact]
        public void DeleteJoins()
        {
            var query = new Query(TestTableUtils.TABLE + " t1");
            query.JoinToDelete("t2").Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            Assert.Throws<NotSupportedException>(() => query.Grammar().Delete());
        }
    }
}
