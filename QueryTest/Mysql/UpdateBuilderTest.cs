using BaseTest.Models;
using BaseTest.Utils;
using BaseTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Mysql
{
    public class UpdateBuilderTest(ITestOutputHelper output, MockFixture<MysqlQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<MysqlQueryConfig>>, IUpdateBuilderTest
    {
        [Theory]
        [InlineData(Trashed.With, "")]
        [InlineData(Trashed.Except, " WHERE `deleted` = 0")]
        [InlineData(Trashed.Only, " WHERE `deleted` = 1")]
        public void UpdateSoftDeleted(Trashed visibility, string expectedWhere)
        {
            using var query = new Query<SoftDeleteDateAddress> { Trashed = visibility };
            Cell[] cells = [new Cell("Name", "My Name"), new Cell("Street", "My Street")];

            QueryAssert.Equal($"UPDATE `SoftDeleteDateAddress` SET `Name` = ?, `Street` = ?{expectedWhere}", query.Grammar().Update(cells));
        }

        [Fact]
        public void Update()
        {
            using var query = new Query(TestTableUtils.TABLE);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));

            QueryAssert.EqualDecoded(
                "UPDATE `TestTable` SET `name` = @p1, `alias` = @p2, `value` = NULL, `status` = 1",
                ["MyTestName", "Test"],
                query.Grammar().Update(row.Cells)
            );
        }

        [Fact]
        public void UpdateByColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("id", "=", 1);

            QueryAssert.Equal(
                "UPDATE `TestTable` SET `name` = `nick` WHERE `id` = 1",
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

            QueryAssert.EqualDecoded(
                "UPDATE `TestTable` SET `name` = @p1, `alias` = CASE WHEN `alias` IS NULL THEN @p2 ELSE @p3 END, `value` = NULL, `status` = 1",
                ["MyTestName", CaseMsg, ElseMsg],
                query.Grammar().Update([new Cell("name", "MyTestName"), new Cell("alias", caseVal), new Cell("value", null), new Cell("status", Status.Success)])
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

            QueryAssert.EqualDecoded(
                "UPDATE `TestTable` SET `name` = @p1 WHERE `id` = 1",
                ["MyName"],
                query.Grammar().Update([new Cell("name", "MyName")])
            );
        }

        [Fact]
        public void UpdateWhereJoin()
        {
            using var query = new Query(TestTableUtils.TABLE + " t1");
            query.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            query.Where("t2.Id", 1);

            QueryAssert.EqualDecoded(
                "UPDATE `TestTable` `t1` INNER JOIN `Table2` `t2` ON `t2`.`Id` = `t1`.`T2Id` SET `name` = @p1, `alias` = @p2, `value` = NULL, `status` = 1 WHERE `t2`.`Id` = 1",
                ["MyTestName", "Test"],
                query.Grammar().Update([new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success)])
            );
        }
    }
}
