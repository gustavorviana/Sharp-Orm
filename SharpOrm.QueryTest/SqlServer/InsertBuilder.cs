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
    public class InsertBuilder(ITestOutputHelper output, DbFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<DbFixture<SqlServerQueryConfig>>, IInsertBuilderTest
    {
        [Fact]
        public void BulkInsert()
        {
            using var query = new Query(TestTableUtils.TABLE);
            var expression = query.Grammar().BulkInsert(
                [
                    NewRow(1, "T1"),
                    NewRow(2, "T2"),
                    NewRow(3, "T3"),
                    NewRow(4, "T4"),
                    NewRow(5, "T5")
                ]
            );

            QueryAssert.Equal(
                expression,
                new SqlExpression("INSERT INTO [TestTable] ([id], [name]) VALUES (1, ?), (2, ?), (3, ?), (4, ?), (5, ?)", "T1", "T2", "T3", "T4", "T5")
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

            var expression = query.Grammar().Insert([new Cell(TestTableUtils.ID, 1), new Cell(TestTableUtils.NAME, "T1"), new Cell("value", null)]);

            QueryAssert.Equal(
                new SqlExpression("INSERT INTO [TestTable] ([id], [name], [value]) VALUES (1, ?, NULL); SELECT SCOPE_IDENTITY();", "T1"),
                expression
            );
        }

        [Fact]
        public void InsertByBasicSelect()
        {
            using var selectQuery = new Query("User");
            selectQuery.Select(new Column("Id"), (Column)"1").Where("id", 1);

            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().InsertQuery(selectQuery, ["UserId", "Status"]);
            QueryAssert.Equal("INSERT INTO [TestTable] ([UserId], [Status]) SELECT [Id], 1 FROM [User] WHERE [id] = 1", sqlExpression);
        }

        [Fact]
        public void InsertExtendedClass()
        {
            using var q = new Query(TestTableUtils.TABLE);
            var g = new SqlServerGrammar(q);
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

            var sqlExpression = g.Insert(Row.Parse(table, typeof(TestTable), true, false).Cells);
            QueryAssert.Equal(
                new SqlExpression(
                    "INSERT INTO [TestTable] ([Id], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status]) VALUES (1, ?, NULL, ?, 2.1, ?, 1); SELECT SCOPE_IDENTITY();",
                    table.Name,
                    table.CreatedAt,
                    table.CustomId?.ToString(this.Translation.GuidFormat)
                ),
                sqlExpression
            );
        }

        [Fact]
        public void InsertWithoutId()
        {
            using var query = new Query(TestTableUtils.TABLE);

            query.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");
            query.ReturnsInsetionId = false;


            var sqlExpression = query.Grammar().Insert([new Cell(TestTableUtils.ID, 1), new Cell(TestTableUtils.NAME, "T1"), new Cell("value", null)]);
            QueryAssert.Equal(
                new SqlExpression("INSERT INTO [TestTable] ([id], [name], [value]) VALUES (1, ?, NULL)", "T1"),
                sqlExpression
            );
        }

        [Fact]
        public void InsertWIthRaw()
        {
            using var query = new Query(TestTableUtils.TABLE);

            var sqlExpression = query.Grammar().Insert([new Cell(TestTableUtils.ID, (SqlExpression)"1")]);
            QueryAssert.Equal("INSERT INTO [TestTable] ([id]) VALUES (1); SELECT SCOPE_IDENTITY();", sqlExpression);
        }
    }
}
