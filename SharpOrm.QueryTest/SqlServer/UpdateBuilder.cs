using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class UpdateBuilder(ITestOutputHelper output, DbFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<DbFixture<SqlServerQueryConfig>>, IUpdateBuilderTest
    {
        [Fact]
        public void Update()
        {
            using var q = NewQuery(TestTableUtils.TABLE);
            var g = new SqlServerGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = g.Update(row.Cells);

            QueryAssert.Equal(new SqlExpression("UPDATE [TestTable] SET [name] = ?, [alias] = ?, [value] = NULL, [status] = 1", "MyTestName", "Test"), sqlExpression);
        }

        [Fact]
        public void UpdateByColumn()
        {
            using var q = NewQuery(TestTableUtils.TABLE);
            var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update([new Cell("name", new Column("nick"))]);
            QueryAssert.Equal("UPDATE [TestTable] SET [name] = [nick] WHERE [id] = 1", sqlExpression);
        }

        [Fact]
        public void UpdateCaseValue()
        {
            using var query = NewQuery(TestTableUtils.TABLE);
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
            using var query = NewQuery(TestTableUtils.TABLE);
            Assert.Throws<InvalidOperationException>(() => query.Grammar().Update([]));
        }

        [Fact]
        public void UpdateWhere()
        {
            using var query = NewQuery(TestTableUtils.TABLE);
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
            using var query = NewQuery(TestTableUtils.TABLE, "t1");
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
    }
}
