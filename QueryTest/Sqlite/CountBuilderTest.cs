using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class CountBuilderTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>, ICountBuilderTest
    {
        [Fact]
        public void Count()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", query.Grammar().Count());
        }

        [Fact]
        public void CountAllDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Distinct = true;

            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT * FROM \"TestTable\") \"count\"", query.Grammar().Count());
        }

        [Fact]
        public void CountAllOfTableDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE + " t");
            query.Select("t.*").Distinct = true;

            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT \"t\".* FROM \"TestTable\" \"t\") \"count\"", query.Grammar().Count());
        }

        [Fact]
        public void CountDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Distinct = true;

            QueryAssert.Equal(
                "SELECT COUNT(*) FROM (SELECT DISTINCT * FROM \"TestTable\") \"count\"",
                query.Grammar().Count()
            );
        }

        [Fact]
        public void CountJoin()
        {
            using var query = new Query(TestTableUtils.TABLE)
                .Join("TestTable2 t2", "t2.Id", "TestTable.Id2")
                .Select("TestTable.*");

            QueryAssert.Equal(
                "SELECT COUNT(*) FROM \"TestTable\" INNER JOIN \"TestTable2\" \"t2\" ON \"t2\".\"Id\" = \"TestTable\".\"Id2\"",
                query.Grammar().Count()
            );
        }

        [Fact]
        public void CountOffset()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 10;
            query.Offset = 1;
            query.OrderBy(OrderBy.Asc, "Id");

            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", query.Grammar().Count());
        }

        [Fact]
        public void CountMultipleColumnsDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("nick", "name").Distinct = true;

            QueryAssert.Equal(
                "SELECT COUNT(*) FROM (SELECT DISTINCT \"nick\", \"name\" FROM \"TestTable\") \"count\"",
                query.Grammar().Count()
            );
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

            QueryAssert.Equal(
                "SELECT COUNT(DISTINCT \"Column\") FROM \"TestTable\"",
                query.Grammar().Count()
            );
        }

        [Fact]
        public void CountWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Column", null);

            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\" WHERE \"Column\" IS NULL", query.Grammar().Count());
        }

        [Fact]
        public void CountWithOrderBy()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy(OrderBy.Asc, "Id");

            QueryAssert.Equal("SELECT COUNT(*) FROM \"TestTable\"", query.Grammar().Count());
        }

        [Fact]
        public void CountColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.Equal("SELECT COUNT(\"ColName\") FROM \"TestTable\"", query.Grammar().Count(new Column("ColName")));
        }
    }
}
