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
    public class InsertBuilderTest(ITestOutputHelper output, MockFixture<MysqlQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<MysqlQueryConfig>>, IInsertBuilderTest
    {
        [Fact]
        public void BulkInsert()
        {
            using var query = new Query(TestTableUtils.TABLE);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`id`, `name`) VALUES (1, @p1), (2, @p2), (3, @p3), (4, @p4), (5, @p5)",
                 ["T1", "T2", "T3", "T4", "T5"],
                 query.Grammar().BulkInsert(rows)
            );
        }

        private static Row NewRow(int id, string name)
        {
            return new Row(new Cell(TestTableUtils.ID, id), new Cell(TestTableUtils.NAME, name));
        }

        [Fact]
        public void Insert()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`id`, `name`, `value`) VALUES (1, @p1, NULL); SELECT LAST_INSERT_ID();",
                ["T1"],
                query.Grammar().Insert([new Cell(TestTableUtils.ID, 1), new Cell(TestTableUtils.NAME, "T1"), new Cell("value", null)])
            );
        }

        [Fact]
        public void InsertWithoutGenId()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`id`, `name`, `value`) VALUES (1, @p1, NULL)",
                ["T1"],
                query.Grammar().Insert([new Cell(TestTableUtils.ID, 1), new Cell(TestTableUtils.NAME, "T1"), new Cell("value", null)], false)
            );
        }

        [Fact]
        public void InsertByBasicSelect()
        {
            using var selectQuery = new Query("User");
            selectQuery
                .Select(new Column("Id"), (Column)"1")
                .Where("id", 1);

            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().InsertQuery(selectQuery, ["UserId", "Status"]);
            QueryAssert.Equal("INSERT INTO `TestTable` (`UserId`, `Status`) SELECT `Id`, 1 FROM `User` WHERE `id` = 1", sqlExpression);
        }

        [Fact]
        public void InsertByBasicWithColumnNamesSelect()
        {
            using var selectQuery = new Query("User");
            selectQuery
                .Select("Id", "Status")
                .Where("id", 1);

            using var query = new Query(TestTableUtils.TABLE);
            
            var sqlExpression = query.Grammar().InsertQuery(selectQuery, []);
            QueryAssert.Equal("INSERT INTO `TestTable` SELECT `Id`, `Status` FROM `User` WHERE `id` = 1", sqlExpression);
        }

        [Fact]
        public void InsertExtendedClass()
        {
            using var query = new Query(TestTableUtils.TABLE);
            var table = new ExtendedTestTable
            {
                Id = 1,
                CreatedAt = DateTime.Now,
                CustomId = Guid.NewGuid(),
                Name = "Name",
                Number = 2.1M,
                ExtendedProp = "Nothing",
                CustomStatus = Status.Success,
                Nick = null
            };

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`Id`, `Name`, `Nick`, `record_created`, `Number`, `custom_id`, `custom_status`) VALUES (1, @p1, NULL, @p2, 2.1, @p3, 1); SELECT LAST_INSERT_ID();",
                [table.Name, table.CreatedAt, table.CustomId?.ToString(this.Translation.GuidFormat)!],
                query.Grammar().Insert(Row.Parse(table, typeof(TestTable), true, false).Cells)
            );
        }
        
        [Fact]
        public void InsertWithoutId()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.ReturnsInsetionId = false;
            query.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`id`, `name`, `value`) VALUES (1, @p1, NULL)",
                ["T1"],
                query.Grammar().Insert([new Cell(TestTableUtils.ID, 1), new Cell(TestTableUtils.NAME, "T1"), new Cell("value", null)])
            );
        }
        
        [Fact]
        public void InsertWIthRaw()
        {
            using var query = new Query(TestTableUtils.TABLE);

            QueryAssert.EqualDecoded(
                "INSERT INTO `TestTable` (`id`) VALUES (1); SELECT LAST_INSERT_ID();",
                [],
                query.Grammar().Insert([new Cell(TestTableUtils.ID, (SqlExpression)"1")])
            );
        }

        [Fact]
        public void InsertByExpressionSelect()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().InsertExpression(new SqlExpression("SELECT `Id`, 1 FROM `User` WHERE `id` = 1"), ["UserId", "Status"]);
            QueryAssert.Equal("INSERT INTO `TestTable` (`UserId`, `Status`) SELECT `Id`, 1 FROM `User` WHERE `id` = 1", sqlExpression);
        }

        [Fact]
        public void InsertByExpressionWithColumnNamesSelect()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().InsertExpression(new SqlExpression("SELECT `Id`, `Status` FROM `User` WHERE `id` = 1"), []);
            QueryAssert.Equal("INSERT INTO `TestTable` SELECT `Id`, `Status` FROM `User` WHERE `id` = 1", sqlExpression);
        }
    }
}
