using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.SqlServer;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class DeleteBuilder(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>, IDeleteBuilderTest
    {
        [Fact]
        public void SoftDeleteWithDate()
        {
            using var query = new Query<SoftDeleteDateAddress>();
            var tableInfo = Config.Translation.GetTable(typeof(SoftDeleteDateAddress));

            QueryAssert.Equal($"UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = ? WHERE [deleted] = 0", query.Grammar().SoftDelete(tableInfo.SoftDelete));
        }

        [Fact]
        public void RestoreSoftDeletedWithDate()
        {
            using var query = new Query<SoftDeleteDateAddress>();
            var tableInfo = Config.Translation.GetTable(typeof(SoftDeleteDateAddress));

            var result = query.Grammar().RestoreSoftDeleted(tableInfo.SoftDelete);
            QueryAssert.Equal($"UPDATE [SoftDeleteDateAddress] SET [deleted] = 0, [deleted_at] = NULL WHERE [deleted] = 1", result);
        }

        [Fact]
        public void SoftDelete()
        {
            using var query = new Query<SoftDeleteAddress>();
            var tableInfo = Config.Translation.GetTable(typeof(SoftDeleteAddress));

            QueryAssert.Equal($"UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0", query.Grammar().SoftDelete(tableInfo.SoftDelete));
        }

        [Fact]
        public void RestoreSoftDeleted()
        {
            using var query = new Query<SoftDeleteAddress>();
            var tableInfo = Config.Translation.GetTable(typeof(SoftDeleteAddress));

            var result = query.Grammar().RestoreSoftDeleted(tableInfo.SoftDelete);
            QueryAssert.Equal($"UPDATE [SoftDeleteAddress] SET [deleted] = 0 WHERE [deleted] = 1", result);
        }

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
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            Assert.Throws<NotSupportedException>(() => query.Grammar().DeleteIncludingJoins(["t2"]));
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

        [Fact]
        public void DeleteWithLockHintRowLock()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { LockHint = SqlServerLockHint.RowLock };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (ROWLOCK)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithLockHintTabLock()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { LockHint = SqlServerLockHint.TabLock };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (TABLOCK)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithLockHintTabLockX()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { LockHint = SqlServerLockHint.TabLockX };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (TABLOCKX)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithConcurrencyReadPast()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { Concurrency = SqlServerConcurrencyHint.ReadPast };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (READPAST)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithConcurrencyNoWait()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { Concurrency = SqlServerConcurrencyHint.NoWait };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (NOWAIT)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithPlanHintForceSeek()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { PlanHint = SqlServerPlanHint.ForceSeek };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (FORCESEEK)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithPlanHintForceScan()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { PlanHint = SqlServerPlanHint.ForceScan };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (FORCESCAN)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithIndex()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { Index = "IX_TestTable_Id" };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (INDEX(IX_TestTable_Id))", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithMultipleHints()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions 
            { 
                LockHint = SqlServerLockHint.RowLock,
                Concurrency = SqlServerConcurrencyHint.ReadPast | SqlServerConcurrencyHint.NoWait,
                PlanHint = SqlServerPlanHint.ForceSeek
            };

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (ROWLOCK, READPAST, NOWAIT, FORCESEEK)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithReadIsolationShouldNotIncludeInDelete()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions 
            { 
                ReadIsolation = SqlServerReadIsolationHint.NoLock,
                LockHint = SqlServerLockHint.RowLock
            };

            // ReadIsolation should NOT appear in DELETE, only LockHint
            QueryAssert.Equal("DELETE FROM [TestTable] WITH (ROWLOCK)", query.Grammar().Delete());
        }

        [Fact]
        public void DeleteWithWhereAndHints()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GrammarOptions = new SqlServerGrammarOptions { LockHint = SqlServerLockHint.RowLock };
            query.Where("id", "=", 1);

            QueryAssert.Equal("DELETE FROM [TestTable] WITH (ROWLOCK) WHERE [id] = 1", query.Grammar().Delete());
        }
    }
}
