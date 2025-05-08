using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Fb;
using Xunit.Abstractions;

namespace QueryTest.Firebird
{
    public class CountBuilderTest(ITestOutputHelper output, MockFixture<FbQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<FbQueryConfig>>, ICountBuilderTest
    {
        [Fact]
        public void Count()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", sqlExpression);
        }

        [Fact]
        public void CountDistinctWithOrder()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("nick", "name").Distinct = true;
            query.Limit = 10;
            query.Offset = 1;
            query.OrderBy(OrderBy.Asc, "Id");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT \"nick\", \"name\" FROM \"TestTable\") \"count\"", sqlExpression);
        }

        [Fact]
        public void CountOneColumnDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("Column").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(DISTINCT \"Column\") FROM \"TestTable\"", sqlExpression);
        }

        [Fact]
        public void CountMultipleColumnsDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("nick", "name").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT \"nick\", \"name\" FROM \"TestTable\") \"count\"", sqlExpression);
        }

        [Fact]
        public void CountAllDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT * FROM \"TestTable\") \"count\"", sqlExpression);
        }

        [Fact]
        public void CountAllOfTableDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE + " t");
            query.Select("t.*").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT \"t\".* FROM \"TestTable\" \"t\") \"count\"", sqlExpression);
        }

        [Fact]
        public void CountColumnWithOrderBy()
        {
            using var query = new Query<TestTable>();
            query.OrderBy(TestTableUtils.NAME);
            query.Select(TestTableUtils.NAME);
            query.Distinct = true;

            var sqlExpression = query.Grammar().Count((Column)"COUNT(DISTINCT name)");
            QueryAssert.Equal("SELECT COUNT(DISTINCT name) FROM \"TestTable\"", sqlExpression);
        }

        [Fact]
        public void CountWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Column", null);

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\" WHERE \"Column\" IS NULL", sqlExpression);
        }


        [Fact]
        public void CountOffset()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 10;
            query.Offset = 1;
            query.OrderBy(OrderBy.Asc, "Id");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", sqlExpression);
        }

        [Fact]
        public void CountJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.EqualDecoded(
                "SELECT COUNT(*) FROM \"TestTable\" INNER JOIN \"Table2\" \"t2\" ON \"t2\".\"IdTable\" = \"TestTable\".\"Id\" WHERE \"t2\".\"Column\" = @p1",
                ["Value"],
                sqlExpression
            );
        }

        [Fact]
        public void CountWithOrderBy()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy(OrderBy.Asc, "Id");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", sqlExpression);
        }

        [Fact]
        public void CountColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().Count(new Column("ColName"));
            QueryAssert.Equal("SELECT COUNT(\"ColName\") FROM \"TestTable\"", sqlExpression);
        }
    }
}
