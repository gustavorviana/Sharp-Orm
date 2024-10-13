﻿using BaseTest.Utils;
using QueryTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class DeleteBuilder(ITestOutputHelper output, DbFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<DbFixture<SqlServerQueryConfig>>, IDeleteBuilderTest
    {
        [Fact]
        public void Delete()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.Equal("DELETE FROM [TestTable]", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteJoins()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.JoinToDelete("t2").Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            Assert.Throws<NotSupportedException>(query.Grammar().Delete);
        }

        [Fact]
        public void DeleteLimit()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Limit = 5;

            QueryAssert.Equal("DELETE TOP(5) FROM [TestTable]", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteOrder()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.OrderBy("id");

            QueryAssert.Equal("DELETE FROM [TestTable]", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            QueryAssert.Equal("DELETE FROM [TestTable] WHERE [id] = 1", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWhereJoin()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            QueryAssert.Equal("DELETE [t1] FROM [TestTable] [t1] INNER JOIN [Table2] [t2] ON [t2].[Id] = [t1].[T2Id] WHERE [t2].[Id] = 1", query.Grammar().Delete());
        }
    }
}