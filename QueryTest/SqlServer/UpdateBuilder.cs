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
    public class UpdateBuilder(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>, IUpdateBuilderTest
    {
        [Theory]
        [InlineData(Trashed.With, "")]
        [InlineData(Trashed.Except, " WHERE [deleted] = 0")]
        [InlineData(Trashed.Only, " WHERE [deleted] = 1")]
        public void UpdateSoftDeleted(Trashed visibility, string expectedWhere)
        {
            using var query = new Query<SoftDeleteDateAddress> { Trashed = visibility };
            Cell[] cells = [new Cell("Name", "My Name"), new Cell("Street", "My Street")];

            QueryAssert.Equal($"UPDATE [SoftDeleteDateAddress] SET [Name] = ?, [Street] = ?{expectedWhere}", query.Grammar().Update(cells));
        }

        [Fact]
        public void Update()
        {
            using var q = new Query(TestTableUtils.TABLE);
            var g = new SqlServerGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = g.Update(row.Cells);

            QueryAssert.Equal(new SqlExpression("UPDATE [TestTable] SET [name] = ?, [alias] = ?, [value] = NULL, [status] = 1", "MyTestName", "Test"), sqlExpression);
        }

        [Fact]
        public void UpdateByColumn()
        {
            using var q = new Query(TestTableUtils.TABLE);
            var g = new SqlServerGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update([new Cell("name", new Column("nick"))]);
            QueryAssert.Equal("UPDATE [TestTable] SET [name] = [nick] WHERE [id] = 1", sqlExpression);
        }

        [Fact]
        public void UpdateCaseValue()
        {
            using var query = new Query(TestTableUtils.TABLE);
            const string CaseMsg = "Without alias";
            const string ElseMsg = "With alias";

            var caseVal = new Case().When("alias", "IS", null, CaseMsg).Else(ElseMsg);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", caseVal), new Cell("value", null), new Cell("status", Status.Success));

            QueryAssert.Equal(
                query.Grammar().Update(row.Cells),
                new SqlExpression(
                    "UPDATE [TestTable] SET [name] = ?, [alias] = CASE WHEN [alias] IS NULL THEN ? ELSE ? END, [value] = NULL, [status] = 1",
                    row[0].Value,
                    CaseMsg,
                    ElseMsg
                )
            );
        }

        [Fact]
        public void UpdateNoColumns()
        {
            using var query = new Query(TestTableUtils.TABLE);
            Assert.Throws<InvalidOperationException>(() => query.Grammar().Update([]));
        }

        [Fact]
        public void UpdateWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            var sqlExpression = query.Grammar().Update([new Cell("name", "MyName")]);
            QueryAssert.Equal(
                new SqlExpression(
                    "UPDATE [TestTable] SET [name] = ? WHERE [id] = 1",
                    "MyName"
                ),
                sqlExpression
            );
        }

        [Fact]
        public void UpdateWhereJoin()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = query.Grammar().Update(row.Cells);

            QueryAssert.Equal(
                new SqlExpression(
                    "UPDATE [t1] SET [name] = ?, [alias] = ?, [value] = NULL, [status] = 1 FROM [TestTable] [t1] INNER JOIN [Table2] [t2] ON [t2].[Id] = [t1].[T2Id] WHERE [t2].[Id] = 1",
                    "MyTestName",
                    "Test"
                ),
                sqlExpression
            );
        }

        [Fact]
        public void UpdateJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Join("Table2 t2", "t2.Id", "=", "TestTable.T2Id");
            query.Where("t2.Id", 1);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = query.Grammar().Update(row.Cells);

            QueryAssert.Equal(
                new SqlExpression(
                    "UPDATE [TestTable] SET [name] = ?, [alias] = ?, [value] = NULL, [status] = 1 FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[Id] = [TestTable].[T2Id] WHERE [t2].[Id] = 1",
                    "MyTestName",
                    "Test"
                ),
                sqlExpression
            );
        }
    }
}
