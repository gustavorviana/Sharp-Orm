using BaseTest.Models;
using BaseTest.Utils;
using BaseTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class UpdateBuilderTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>, IUpdateBuilderTest
    {
        [Theory]
        [InlineData(Trashed.With, "")]
        [InlineData(Trashed.Except, " WHERE \"deleted\" = 0")]
        [InlineData(Trashed.Only, " WHERE \"deleted\" = 1")]
        public void UpdateSoftDeleted(Trashed visibility, string expectedWhere)
        {
            using var query = new Query<SoftDeleteDateAddress> { Trashed = visibility };
            Cell[] cells = [new Cell("Name", "My Name"), new Cell("Street", "My Street")];

            QueryAssert.Equal($"UPDATE \"SoftDeleteDateAddress\" SET \"Name\" = ?, \"Street\" = ?{expectedWhere}", query.Grammar().Update(cells));
        }

        [Fact]
        public void Update()
        {
            using var query = new Query(TestTableUtils.TABLE);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));

            QueryAssert.Equal(
                "UPDATE \"TestTable\" SET \"name\" = ?, \"alias\" = ?, \"value\" = NULL, \"status\" = 1",
                query.Grammar().Update(row.Cells)
            );
        }

        [Fact]
        public void UpdateByColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            QueryAssert.Equal(
                "UPDATE \"TestTable\" SET \"name\" = \"nick\" WHERE \"id\" = 1",
                query.Grammar().Update([new Cell("name", new Column("nick"))])
            );
        }

        [Fact]
        public void UpdateCaseValue()
        {
            using var query = new Query(TestTableUtils.TABLE);

            const string CaseMsg = "Without alias";
            const string ElseMsg = "With alias";

            var caseVal = new Case().When("alias", "IS", null, CaseMsg).Else(ElseMsg);

            QueryAssert.Equal(
                "UPDATE \"TestTable\" SET \"name\" = ?, \"alias\" = CASE WHEN \"alias\" IS NULL THEN ? ELSE ? END, \"value\" = NULL, \"status\" = 1",
                query.Grammar().Update(
                [
                    new Cell("name", "MyTestName"),
                    new Cell("alias", caseVal),
                    new Cell("value", null),
                    new Cell("status", Status.Success)
                ])
            );
        }

        [Fact]
        public void UpdateNoColumns()
        {
            using var query = new Query(TestTableUtils.TABLE);

            Assert.Throws<InvalidOperationException>(() => query.Grammar().Update(Array.Empty<Cell>()));
        }

        [Fact]
        public void UpdateWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            QueryAssert.Equal(
                "UPDATE \"TestTable\" SET \"name\" = ? WHERE \"id\" = 1",
                query.Grammar().Update([new Cell("name", "MyName")])
            );
        }

        [Fact]
        public void UpdateWhereJoin()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);
            Cell[] cells = [
                    new Cell("name", "MyTestName"),
                    new Cell("alias", "Test"),
                    new Cell("value", null),
                    new Cell("status", Status.Success)
                ];

            Assert.Throws<NotSupportedException>(() => query.Grammar().Update(cells));
        }
    }
}
