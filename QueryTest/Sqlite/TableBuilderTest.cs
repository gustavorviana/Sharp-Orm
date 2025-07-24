using BaseTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class TableBuilderTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>, ITableBuilderTest
    {
        [Fact]
        public void ExistsTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = \"MyTable\";");

            Assert.Equal(expected, grammar.Exists());
        }

        [Fact]
        public void ExistsTempTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("SELECT COUNT(*) FROM sqlite_temp_master WHERE type='table' AND name = \"temp_MyTable\";");

            Assert.Equal(expected, grammar.Exists());
        }

        [Fact]
        public void DropTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE \"MyTable\"");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void DropTempTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TABLE \"temp_MyTable\"");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void CreateBasedTable()
        {
            var q = Query.ReadOnly("BaseTable", Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = GetTableGrammar(new TableSchema("MyTable", q));
            var expected = new SqlExpression("CREATE TABLE \"MyTable\" AS SELECT \"Id\", \"Name\" FROM \"BaseTable\" WHERE \"Id\" > 50");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateBasedTempTable()
        {
            var q = Query.ReadOnly("BaseTable", Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = GetTableGrammar(new TableSchema("MyTable", q) { Temporary = true });
            var expected = new SqlExpression("CREATE TABLE temp.\"temp_MyTable\" AS SELECT \"Id\", \"Name\" FROM \"BaseTable\" WHERE \"Id\" > 50");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateTable()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.Add<string>("Name");
            cols.Add<int>("Status").Unique = true;
            cols.Add<int>("Status2").Unique = true;

            var grammar = GetTableGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE \"MyTable\"(\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,\"Name\" TEXT NULL,\"Status\" INTEGER NULL,\"Status2\" INTEGER NULL,CONSTRAINT \"UC_MyTable_Status_Status2\" UNIQUE (\"Status\",\"Status2\"))");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id");
            cols.AddPk("Id2");

            var grammar = GetTableGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE \"MyTable\"(\"Id\" INTEGER NOT NULL,\"Id2\" INTEGER NOT NULL,PRIMARY KEY (\"Id\",\"Id2\"))");

            Assert.Equal(expected, grammar.Create());
        }
    }
}
