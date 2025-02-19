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
    public class DeleteBuilderTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>, IDeleteBuilderTest
    {
        [Fact]
        public void SoftDeleteWithDate()
        {
            using var query = new Query<SoftDeleteDateAddress>();
            var tableInfo = this.Config.Translation.GetTable(typeof(SoftDeleteDateAddress));

            QueryAssert.Equal($"UPDATE \"SoftDeleteDateAddress\" SET \"deleted\" = 1, \"deleted_at\" = ? WHERE \"deleted\" = 0", query.Grammar().SoftDelete(tableInfo.SoftDelete));
        }

        [Fact]
        public void RestoreSoftDeletedWithDate()
        {
            using var query = new Query<SoftDeleteDateAddress>();
            var tableInfo = this.Config.Translation.GetTable(typeof(SoftDeleteDateAddress));

            var result = query.Grammar().RestoreSoftDeleted(tableInfo.SoftDelete);
            QueryAssert.Equal($"UPDATE \"SoftDeleteDateAddress\" SET \"deleted\" = 0, \"deleted_at\" = NULL WHERE \"deleted\" = 1", result);
        }

        [Fact]
        public void SoftDelete()
        {
            using var query = new Query<SoftDeleteAddress>();
            var tableInfo = this.Config.Translation.GetTable(typeof(SoftDeleteAddress));

            QueryAssert.Equal($"UPDATE \"SoftDeleteAddress\" SET \"deleted\" = 1 WHERE \"deleted\" = 0", query.Grammar().SoftDelete(tableInfo.SoftDelete));
        }

        [Fact]
        public void RestoreSoftDeleted()
        {
            using var query = new Query<SoftDeleteAddress>();
            var tableInfo = this.Config.Translation.GetTable(typeof(SoftDeleteAddress));

            var result = query.Grammar().RestoreSoftDeleted(tableInfo.SoftDelete);
            QueryAssert.Equal($"UPDATE \"SoftDeleteAddress\" SET \"deleted\" = 0 WHERE \"deleted\" = 1", result);
        }

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
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            Assert.Throws<NotSupportedException>(() => query.Grammar().DeleteIncludingJoins(["t2"]));
        }
    }
}
