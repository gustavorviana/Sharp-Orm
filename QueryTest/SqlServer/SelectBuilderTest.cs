﻿using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class SelectBuilderTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        [Theory]
        [InlineData(Trashed.With, "")]
        [InlineData(Trashed.Except, " WHERE [deleted] = 0")]
        [InlineData(Trashed.Only, " WHERE [deleted] = 1")]
        public void SelectSoftDeleted(Trashed visibility, string expectedWhere)
        {
            using var query = new Query<SoftDeleteDateAddress> { Trashed = visibility };

            QueryAssert.Equal($"SELECT * FROM [SoftDeleteDateAddress]{expectedWhere}", query.Grammar().Select());
        }

        [Theory]
        [InlineData("colName", "[colName]")]
        [InlineData("table.colName", "[table].[colName]")]
        [InlineData("*", "*")]
        [InlineData("table.*", "[table].*")]
        public void FixColumnName(string raw, string expected)
        {
            Assert.Equal(expected, this.Config.ApplyNomenclature(raw));
        }

        [Fact]
        public void SelectByLambda()
        {
            using var query = new Query<Address>();
            query.Select(x => x.Street);

            QueryAssert.Equal($"SELECT [Street] FROM [Address]", query.Grammar().Select());
        }

        [Fact]
        public void SelectByMultipleLambdas()
        {
            using var query = new Query<Address>();
            query.Select(x => new { x.Id, x.Street, x.Name });

            QueryAssert.Equal($"SELECT [Id], [Street], [Name] FROM [Address]", query.Grammar().Select());
        }

        [Fact]
        public void OrderByLambda()
        {
            using var query = new Query<Address>();
            query.OrderBy(x => x.Name);

            QueryAssert.Equal($"SELECT * FROM [Address] ORDER BY [Name] ASC", query.Grammar().Select());
        }

        [Fact]
        public void OrderByMultipleLambdas()
        {
            using var query = new Query<Address>();
            query.OrderBy(x => new { x.Name, x.Street });

            QueryAssert.Equal($"SELECT * FROM [Address] ORDER BY [Name] ASC, [Street] ASC", query.Grammar().Select());
        }

        [Fact]
        public void GroupByLambda()
        {
            using var query = new Query<Address>();
            query.GroupBy(x => x.Name);

            QueryAssert.Equal($"SELECT * FROM [Address] GROUP BY [Name]", query.Grammar().Select());
        }

        [Fact]
        public void GroupByLambdaColumnLowerCase()
        {
            using var query = new Query<Address>();
            query.GroupBy(x => x.Name.ToLower());

            QueryAssert.Equal($"SELECT * FROM [Address] GROUP BY LOWER([Name])", query.Grammar().Select());
        }

        [Fact]
        public void Select()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.Equal("SELECT * FROM [TestTable]", query.Grammar().Select());
        }

        [Fact]
        public void SelectAndOrderBy()
        {
            using var query = new Query(TestTableUtils.TABLE).OrderBy("Id");

            QueryAssert.Equal("SELECT * FROM [TestTable] ORDER BY [Id] ASC", query.Grammar().Select());
        }

        [Fact]
        public void NewSelectOffset()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] ASC OFFSET 1 ROWS", sqlExpression);
        }

        [Fact]
        public void NewSelectOffsetLimit()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("Id");
            query.Where("Id", 1);
            query.Offset = 1;
            query.Limit = 10;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] ASC OFFSET 1 ROWS FETCH NEXT 10 ROWS ONLY", sqlExpression);
        }

        [Fact]
        public void SelectLimitWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("Id").Limit = 1;
            query.Where("Id", 1);

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT TOP(1) * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] ASC", sqlExpression);
        }

        [Fact]
        public void SelectLimit()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 1;

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT TOP(1) * FROM [TestTable]", sqlExpression);
        }

        [Fact]
        public void SelectWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Id", 1);

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] WHERE [Id] = 1", sqlExpression);
        }

        [Fact]
        public void CountDistinctSelect2()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("Column").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(DISTINCT [Column]) FROM [TestTable]", sqlExpression);
        }

        [Fact]
        public void CountDistinctSelect3()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("nick", "name").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT [nick], [name] FROM [TestTable]) AS [count]", sqlExpression);
        }

        [Fact]
        public void CountAllDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT * FROM [TestTable]) AS [count]", sqlExpression);
        }

        [Fact]
        public void CountAllOfTableDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE + " t");
            query.Select("t.*").Distinct = true;

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM (SELECT DISTINCT [t].* FROM [TestTable] [t]) AS [count]", sqlExpression);
        }

        [Fact]
        public void PaginateDistinctColumn()
        {
            using var query = new Query<TestTable>();
            query.OrderBy(TestTableUtils.NAME);
            query.Distinct = true;
            query.Select(TestTableUtils.NAME);

            var sqlExpression = query.Grammar().Count((Column)"COUNT(DISTINCT name)");
            QueryAssert.Equal("SELECT COUNT(DISTINCT name) FROM [TestTable]", sqlExpression);
        }

        [Fact]
        public void CountSelect()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM [TestTable]", sqlExpression);
        }

        [Fact]
        public void CountWhereSelect()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Column", null);

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM [TestTable] WHERE [Column] IS NULL", sqlExpression);
        }

        [Fact]
        public void SelectJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Join("Table2 t2", q => q.WhereColumn("t2.IdTable", "TestTable.Id"), grammarOptions: new SqlServerGrammarOptions { NoLock = true });

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM [TestTable] INNER JOIN [Table2] [t2] WITH (NOLOCK) ON [t2].[IdTable] = [TestTable].[Id]", sqlExpression);
        }

        [Fact]
        public void CountSelectJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.EqualDecoded(
                "SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1",
                ["Value"],
                sqlExpression
            );
        }

        [Fact]
        public void CountNewSelectJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.EqualDecoded(
                "SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1",
                ["Value"],
                sqlExpression
            );
        }

        [Fact]
        public void DeleteWithNoLock()
        {
            using var query = new Query(new DbName(TestTableUtils.TABLE, "T"));
            query.EnableNoLock();

            QueryAssert.Equal("DELETE [T] FROM [TestTable] [T] WITH (NOLOCK)", query.Grammar().Delete());
        }

        [Fact]
        public void CountOffset()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 10;
            query.Offset = 1;
            query.OrderBy(OrderBy.Asc, "Id");

            var sqlExpression = query.Grammar().Count();
            QueryAssert.Equal("SELECT COUNT(*) FROM [TestTable]", sqlExpression);
        }

        [Fact]
        public void SelectGroupByColumnName()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GroupBy("Col1", "Col2");

            QueryAssert.Equal("SELECT * FROM [TestTable] GROUP BY [Col1], [Col2]", query.Grammar().Select());
        }

        [Fact]
        public void SelectHavingColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GroupBy("Col1", "Col2").Having(q => q.Where("Col1", true));

            QueryAssert.Equal("SELECT * FROM [TestTable] GROUP BY [Col1], [Col2] HAVING [Col1] = 1", query.Grammar().Select());
        }
    }
}
