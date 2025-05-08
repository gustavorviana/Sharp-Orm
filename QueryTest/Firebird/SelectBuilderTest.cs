using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Fb;
using Xunit.Abstractions;

namespace QueryTest.Firebird
{
    public class SelectBuilderTest(ITestOutputHelper output, MockFixture<FbQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<FbQueryConfig>>, ISelectBuilderTests
    {
        [Theory]
        [InlineData(Trashed.With, "")]
        [InlineData(Trashed.Except, " WHERE \"deleted\" = 0")]
        [InlineData(Trashed.Only, " WHERE \"deleted\" = 1")]
        public void SelectSoftDeleted(Trashed visibility, string expectedWhere)
        {
            using var query = new Query<SoftDeleteDateAddress> { Trashed = visibility };

            QueryAssert.Equal($"SELECT * FROM \"SoftDeleteDateAddress\"{expectedWhere}", query.Grammar().Select());
        }

        [Fact]
        public void SelectByLambda()
        {
            using var query = new Query<Address>();
            query.Select(x => x.Street);

            QueryAssert.Equal($"SELECT \"Street\" FROM \"Address\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectByMultipleLambdas()
        {
            using var query = new Query<Address>();
            query.Select(x => new { x.Id, x.Street, x.Name });

            QueryAssert.Equal($"SELECT \"Id\", \"Street\", \"Name\" FROM \"Address\"", query.Grammar().Select());
        }

        [Fact]
        public void OrderByLambda()
        {
            using var query = new Query<Address>();
            query.OrderBy(x => x.Name);

            QueryAssert.Equal($"SELECT * FROM \"Address\" ORDER BY \"Name\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void OrderByMultipleLambdas()
        {
            using var query = new Query<Address>();
            query.OrderBy(x => new { x.Name, x.Street });

            QueryAssert.Equal($"SELECT * FROM \"Address\" ORDER BY \"Name\" ASC, \"Street\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void GroupByLambda()
        {
            using var query = new Query<Address>();
            query.GroupBy(x => x.Name);

            QueryAssert.Equal($"SELECT * FROM \"Address\" GROUP BY \"Name\"", query.Grammar().Select());
        }

        [Fact]
        public void GroupByLambdaColumnLowerCase()
        {
            using var query = new Query<Address>();
            query.GroupBy(x => x.Name.ToLower());

            QueryAssert.Equal($"SELECT * FROM \"Address\" GROUP BY LOWER(\"Name\")", query.Grammar().Select());
        }

        [Fact]
        public void CaseEmptyCase()
        {
            using var query = new Query(TestTableUtils.TABLE).OrderBy("Name");
            Assert.Throws<InvalidOperationException>(() => new Case().ToExpression(query.Info.ToReadOnly()));
        }

        [Fact]
        public void ColumnCase()
        {
            const string SQL = "CASE \"Column\" WHEN 1 THEN ? WHEN 0 THEN ? END";
            using var query = new Query(TestTableUtils.TABLE).OrderBy("Name");

            var c = new Case("Column", "Alias").When(1, "Yes").When(0, "No");

            QueryAssert.Equal(SQL, c.ToExpression(query.Info.ToReadOnly(), false));
            QueryAssert.Equal($"{SQL} AS \"Alias\"", c.ToExpression(query.Info.ToReadOnly()));
        }

        [Fact]
        public void ColumnCaseExpression()
        {
            using var query = new Query(TestTableUtils.TABLE).OrderBy("Name");

            var c = new Case().When("Column", ">=", "10", "No").When((SqlExpression)"\"Column\" BETWEEN 11 AND 12", "InRange");
            var exp = c.ToExpression(query.Info.ToReadOnly(), false);
            QueryAssert.EqualDecoded("CASE WHEN \"Column\" >= @p1 THEN @p2 WHEN \"Column\" BETWEEN 11 AND 12 THEN @p3 END", ["10", "No", "InRange"], exp);
        }

        [Fact]
        public void FixColumnName()
        {
            var config = new FbQueryConfig(false);
            string basic = config.ApplyNomenclature("colName");
            string withTable = config.ApplyNomenclature("table.colName");
            string all = config.ApplyNomenclature("*");
            string allWithTable = config.ApplyNomenclature("table.*");

            Assert.Equal("\"colName\"", basic);
            Assert.Equal("\"table\".\"colName\"", withTable);
            Assert.Equal("*", all);
            Assert.Equal("\"table\".*", allWithTable);
        }

        [Fact]
        public void Select()
        {
            using var query = new Query<TestTable>();

            QueryAssert.Equal("SELECT * FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void Select2()
        {
            using var query = new Query("TestTable table").Select("table.*");
            QueryAssert.Equal("SELECT \"table\".* FROM \"TestTable\" \"table\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectCase()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select(new Case(null, "Col").WhenNull("Column", "No value").When("Column2", 2, null).Else((Column)"Column + ' ' + Column2"));

            QueryAssert.EqualDecoded(
                "SELECT CASE WHEN \"Column\" IS NULL THEN @p1 WHEN \"Column2\" = 2 THEN NULL ELSE Column + ' ' + Column2 END AS \"Col\" FROM \"TestTable\"",
                ["No value"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectCase2()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select(new Case(null, "Col").When((Column)"\"Column\" IS NOT NULL", 1).Else((Column)"\"Column\" + ' ' + \"Column2\""));

            QueryAssert.Equal("SELECT CASE WHEN \"Column\" IS NOT NULL THEN 1 ELSE \"Column\" + ' ' + \"Column2\" END AS \"Col\" FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select(new Column("Id"), new Column("Name", "meuNome"));

            QueryAssert.Equal("SELECT \"Id\", \"Name\" AS \"meuNome\" FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectColumnsName()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select("Id", "Name");

            QueryAssert.Equal("SELECT \"Id\", \"Name\" FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectDecimalSqlExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(
                new SqlExpression(
                 "\"Int\" = ? AND \"Long\" = ? AND \"Byte\" = ? AND \"Sbyte\" = ? AND \"Short\" = ? AND \"Ushort\" = ? AND \"Uint\" = ? AND \"Ulong\" = ?",
                 1, 2L, (byte)3, (sbyte)4, (short)5, (ushort)6, 7u, (ulong)8
                )
            );

            QueryAssert.Equal(
                "SELECT * FROM \"TestTable\" WHERE \"Int\" = 1 AND \"Long\" = 2 AND \"Byte\" = 3 AND \"Sbyte\" = 4 AND \"Short\" = 5 AND \"Ushort\" = 6 AND \"Uint\" = 7 AND \"Ulong\" = 8",
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectDistinct()
        {
            using var query = new Query(TestTableUtils.TABLE) { Distinct = true };

            QueryAssert.Equal("SELECT DISTINCT * FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectGroupByColumnName()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GroupBy("Col1", "Col2");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" GROUP BY \"Col1\", \"Col2\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectGroupByColumnObj()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GroupBy(new Column("Col1"), new Column(new SqlExpression("LOWER(Col2)")));

            var sqlExpression = query.Grammar().Select();
            QueryAssert.Equal("SELECT * FROM \"TestTable\" GROUP BY \"Col1\", LOWER(Col2)", sqlExpression);
        }

        [Fact]
        public void SelectGroupByLambdaTest()
        {
            using var query = new Query<TestTable>();
            query.GroupBy(x => x.Name);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" GROUP BY \"Name\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectGroupByLambdaWithJoinTest()
        {
            using var query = new Query<TestTable>();
            query.GroupBy(x => x.Name);
            query.Join("X", "X.Id", "TestTable.Id");

            QueryAssert.Equal(query, "SELECT * FROM \"TestTable\" INNER JOIN \"X\" ON \"X\".\"Id\" = \"TestTable\".\"Id\" GROUP BY \"TestTable\".\"Name\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectHaving()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select((Column)"Phone", (Column)"COUNT(Phone) AS 'PhonesCount'");
            query.GroupBy("Phone");
            query.Having(h => h.Where(new SqlExpression("COUNT(Phone) > 1")));
            query.OrderByDesc("PhonesCount");

            QueryAssert.Equal("SELECT Phone, COUNT(Phone) AS 'PhonesCount' FROM \"TestTable\" GROUP BY \"Phone\" HAVING COUNT(Phone) > 1 ORDER BY \"PhonesCount\" DESC", query.Grammar().Select());
        }

        [Fact]
        public void SelectHavingColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.GroupBy("Col1", "Col2").Having(q => q.Where("Col1", true));

            QueryAssert.Equal("SELECT * FROM \"TestTable\" GROUP BY \"Col1\", \"Col2\" HAVING \"Col1\" = 1", query.Grammar().Select());
        }

        [Fact]
        public void SelectInnerJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Join("TAB2", "TAB2.id", "=", $"{TestTableUtils.TABLE}.idTab2");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" INNER JOIN \"TAB2\" ON \"TAB2\".\"id\" = \"TestTable\".\"idTab2\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectJoinWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Join("TAB2", q => q.WhereColumn("TAB2.id", "=", $"{TestTableUtils.TABLE}.idTab2").OrWhereColumn("TAB2.id", "=", $"{TestTableUtils.TABLE}.idTab3"), "LEFT");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" LEFT JOIN \"TAB2\" ON \"TAB2\".\"id\" = \"TestTable\".\"idTab2\" OR \"TAB2\".\"id\" = \"TestTable\".\"idTab3\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectLeftJoin()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Join("TAB2 tab2", "tab2.id", "=", $"{TestTableUtils.TABLE}.idTab2", "LEFT");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" LEFT JOIN \"TAB2\" \"tab2\" ON \"tab2\".\"id\" = \"TestTable\".\"idTab2\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectLimit()
        {
            using var query = new Query(TestTableUtils.TABLE) { Limit = 10 };

            QueryAssert.Equal("SELECT FIRST 10 * FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectLimitWhere()
        {
            using var query = new Query(TestTableUtils.TABLE) { Limit = 10 };
            query.Where("column", "=", "value");

            QueryAssert.EqualDecoded(
                "SELECT FIRST 10 * FROM \"TestTable\" WHERE \"column\" = @p1",
                ["value"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectMultipleWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("column1", "=", "value1");
            query.Where(e => e.Where("column2", "=", "value2"));

            var sqlExpression = query.Grammar().Select();
            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"column1\" = @p1 AND (\"column2\" = @p2)",
                ["value1", "value2"],
                sqlExpression
            );
        }

        [Fact]
        public void SelectNonDecimalSqlExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(
                new SqlExpression(
                    "\"Int\" = ? AND \"Long\" = ? AND \"Byte\" = ? AND \"Sbyte\" = ? AND \"Short\" = ? AND \"Ushort\" = ? AND \"Uint\" = ? AND \"Ulong\" = ?",
                    1, 2L, (byte)3, (sbyte)4, (short)5, (ushort)6, 7u, (ulong)8
                )
            );

            QueryAssert.Equal(
                "SELECT * FROM \"TestTable\" WHERE \"Int\" = 1 AND \"Long\" = 2 AND \"Byte\" = 3 AND \"Sbyte\" = 4 AND \"Short\" = 5 AND \"Ushort\" = 6 AND \"Uint\" = 7 AND \"Ulong\" = 8",
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectOffsetLimit()
        {
            using var query = new Query(TestTableUtils.TABLE) { Offset = 10, Limit = 10 };

            QueryAssert.Equal("SELECT FIRST 10 SKIP 10 * FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectOrderBy()
        {
            using var query = new Query(TestTableUtils.TABLE + " t").OrderBy("t.Name");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" \"t\" ORDER BY \"t\".\"Name\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void SelectOrderByAlias()
        {
            using var query = new Query(TestTableUtils.TABLE).OrderBy("Name");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" ORDER BY \"Name\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void SelectOrderByLambdaTest()
        {
            using var query = new Query<TestTable>();
            query.OrderBy(x => x.Name);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" ORDER BY \"Name\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void SelectOrderByLambdaWithJoinTest()
        {
            using var query = new Query<TestTable>();
            query.OrderBy(x => x.Name);
            query.Join("X", "X.Id", "TestTable.Id");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" INNER JOIN \"X\" ON \"X\".\"Id\" = \"TestTable\".\"Id\" ORDER BY \"TestTable\".\"Name\" ASC", query.Grammar().Select());
        }

        [Fact]
        public void SelectRawColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Select(new Column("Id"), new Column(new SqlExpression("TOLOWER(Name) AS meuNome")));

            QueryAssert.Equal("SELECT \"Id\", TOLOWER(Name) AS meuNome FROM \"TestTable\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectSqlExpression()
        {
            using var query = new Query<TestTable>();
            const string Name = "Test";

            query.Where(new SqlExpression("\"Name\" = ? AND \"Active\" = ?", Name, true));
            QueryAssert.EqualDecoded("SELECT * FROM \"TestTable\" WHERE \"Name\" = @p1 AND \"Active\" = 1", [Name], query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereBetween()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereBetween("N", 1, 2).OrWhereBetween("N2", 3, 4);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"N\" BETWEEN 1 AND 2 OR \"N2\" BETWEEN 3 AND 4", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereBool()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("First", true).OrWhere("Left", false);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"First\" = 1 OR \"Left\" = 0", query.Grammar().Select());
        }

        [Fact]
        public void SelectIsolatedWhereAndCommonWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(q => q.Where("C1", 1).Where("C2", 2)).OrWhere(q => q.Where("C3", 3).Where("C4", 5));

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE (\"C1\" = 1 AND \"C2\" = 2) OR (\"C3\" = 3 AND \"C4\" = 5)", query.Grammar().Select());
        }

        [Fact]
        public void SelectIsolatedWhere()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(e => e.Where("column", "=", "value"));

            QueryAssert.EqualDecoded("SELECT * FROM \"TestTable\" WHERE (\"column\" = @p1)", ["value"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereColumnsEquals()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("column1", "=", new Column("column2"))
                .Where(new Column("column2"), "=", new Column("column3"));

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"column1\" = \"column2\" AND \"column2\" = \"column3\"", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereContains()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereContains("Title", "10%").OrWhereContains("Title", "pixel");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"Title\" LIKE @p1 OR \"Title\" LIKE @p2",
                ["%10\\%%", "%pixel%"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereEndsWith()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereEndsWith("Title", "30%").OrWhereEndsWith("Title", "80%");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"Title\" LIKE @p1 OR \"Title\" LIKE @p2",
                ["%30\\%", "%80\\%"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereExistsExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Exists(new SqlExpression("1")).OrExists(new SqlExpression("?", "5"));

            QueryAssert.EqualDecoded("SELECT * FROM \"TestTable\" WHERE EXISTS (1) OR EXISTS (@p1)", ["5"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereExistsQuery()
        {
            const string TABLE = "Test";

            using var qTest = new Query(TABLE).Select("Col");
            qTest.Where("Col", ">", 1);

            using var qTest2 = new Query(TABLE).Select("Col");
            qTest2.Where("Col1", "=", "2");

            using var query = new Query(TestTableUtils.TABLE);
            query.Exists(qTest).OrExists(qTest2);

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE EXISTS (SELECT \"Col\" FROM \"Test\" WHERE \"Col\" > 1) OR EXISTS (SELECT \"Col\" FROM \"Test\" WHERE \"Col1\" = @p1)",
                ["2"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereIn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            int[] list = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            query.Where("id", "IN", list);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"id\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9)", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereInColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereInColumn(1, "Status", "Status2").OrWhereInColumn(4, "Status3", "Status4");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE 1 IN (\"Status\", \"Status2\") OR 4 IN (\"Status3\", \"Status4\")", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereInEmpty()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereIn("Status", Array.Empty<int>());

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE 1!=1", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereInExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereIn("N", new SqlExpression("1, ?", "2")).OrWhereIn("N2", new SqlExpression("3, ?", "4"));

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"N\" IN (1, @p1) OR \"N2\" IN (3, @p2)",
                ["2", "4"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereInList()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereIn<int>("N", new List<int> { 1, 2 }).OrWhereIn<string>("N2", new List<string> { "3", "4" });

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"N\" IN (1, 2) OR \"N2\" IN (@p1, @p2)",
                ["3", "4"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereLikeIn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Test", true).WhereLikeIn("Column", "%Name1%", "Name 2%", "%Name 3");

            var sqlExpression = query.Grammar().Select();
            var expectedExp = new SqlExpression("SELECT * FROM \"TestTable\" WHERE \"Test\" = 1 AND (\"Column\" LIKE ? OR \"Column\" LIKE ? OR \"Column\" LIKE ?)", "%Name1%", "Name 2%", "%Name 3");
            QueryAssert.Equal(expectedExp, sqlExpression);
        }

        [Fact]
        public void SelectWhereNot()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNot("Column", 0).OrWhereNot("Column2", "Text");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"Column\" != 0 OR \"Column2\" != @p1",
                ["Text"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereNotBetween()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotBetween("N", 1, 2).OrWhereNotBetween("N2", 3, 4);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"N\" NOT BETWEEN 1 AND 2 OR \"N2\" NOT BETWEEN 3 AND 4", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereNotExistsExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.NotExists(new SqlExpression("1")).OrNotExists(new SqlExpression("?", "5"));

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE NOT EXISTS (1) OR NOT EXISTS (@p1)",
                ["5"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereNotExistsQuery()
        {
            const string TABLE = "Test";

            using var qTest = new Query(TABLE).Select("Col");
            qTest.Where("Col", ">", 1);

            using var qTest2 = new Query(TABLE).Select("Col");
            qTest2.Where("Col1", "=", "2");

            using var query = new Query(TestTableUtils.TABLE);
            query.NotExists(qTest).OrNotExists(qTest2);

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE NOT EXISTS (SELECT \"Col\" FROM \"Test\" WHERE \"Col\" > 1) OR NOT EXISTS (SELECT \"Col\" FROM \"Test\" WHERE \"Col1\" = @p1)",
                ["2"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereNotIn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotIn("Status", 1, 2, 3).OrWhereNotIn("Status2", 3, 4, 5);

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"Status\" NOT IN (1, 2, 3) OR \"Status2\" NOT IN (3, 4, 5)", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereNotInColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotInColumn(1, "Status", "Status2").OrWhereNotInColumn(4, "Status3", "Status4");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE 1 NOT IN (\"Status\", \"Status2\") OR 4 NOT IN (\"Status3\", \"Status4\")", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereNotInExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotIn("N", new SqlExpression("1, ?", "2")).OrWhereNotIn("N2", new SqlExpression("3, ?", "4"));

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"N\" NOT IN (1, @p1) OR \"N2\" NOT IN (3, @p2)",
                ["2", "4"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereNotInList()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotIn<int>("N", new List<int> { 1, 2 }).OrWhereNotIn<string>("N2", new List<string> { "3", "4" });

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"N\" NOT IN (1, 2) OR \"N2\" NOT IN (@p1, @p2)",
                ["3", "4"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereNotLikeIn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Test", true).WhereNotLikeIn("Column", "%Name1%", "Name 2%", "%Name 3");

            var expectedExp = new SqlExpression("SELECT * FROM \"TestTable\" WHERE \"Test\" = 1 AND NOT (\"Column\" LIKE ? OR \"Column\" LIKE ? OR \"Column\" LIKE ?)", "%Name1%", "Name 2%", "%Name 3");
            QueryAssert.Equal(expectedExp, query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereNotNull()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereNotNull("Column").OrWhereNotNull("Column2");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"Column\" IS NOT NULL OR \"Column2\" IS NOT NULL", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereNull()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("Column", null).WhereNull("Column3").OrWhereNull("Column2");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"Column\" IS NULL AND \"Column3\" IS NULL OR \"Column2\" IS NULL", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereOr()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("column", "=", "teste")
                .OrWhere("column", "=", "value");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"column\" = @p1 OR \"column\" = @p2",
                ["teste", "value"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereRawColumn()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where((Column)"UPPER(column1)", "=", "ABC");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE UPPER(column1) = @p1",
                ["ABC"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereRawValue()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where("column1", "=", (SqlExpression)"UPPER(column2)");

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE \"column1\" = UPPER(column2)", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereSqlExpression()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(new SqlExpression("column1 = 1"));

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE column1 = 1", query.Grammar().Select());
        }

        [Fact]
        public void SelectWhereStartsWith()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.WhereStartsWith("Name", "Rod").OrWhereStartsWith("Name", "Mar");

            QueryAssert.EqualDecoded(
                "SELECT * FROM \"TestTable\" WHERE \"Name\" LIKE @p1 OR \"Name\" LIKE @p2",
                ["Rod%", "Mar%"],
                query.Grammar().Select()
            );
        }

        [Fact]
        public void SelectWhereSubCallback()
        {
            using var query = new Query(TestTableUtils.TABLE);
            query.Where(q => q.Where("C1", 1).Where("C2", 2).Where(q => q.Where("C3", 3).Where("C4", 5)));

            QueryAssert.Equal("SELECT * FROM \"TestTable\" WHERE (\"C1\" = 1 AND \"C2\" = 2 AND (\"C3\" = 3 AND \"C4\" = 5))", query.Grammar().Select());
        }
    }
}
