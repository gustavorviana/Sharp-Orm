using BaseTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class TableBuilderTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>, ITableBuilderTest
    {
        [Fact]
        public void ExistsTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ?;", grammar.Name.Name);

            Assert.Equal(expected, grammar.Exists());
        }

        [Fact]
        public void ExistsTempTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE xtype = 'u' AND id = object_id('tempdb..' + ?)", grammar.Name.Name);
            var current = grammar.Exists();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void DropTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE [MyTable]");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void DropTempTableTest()
        {
            var grammar = GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TABLE [#MyTable]");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void CreateBasedTable()
        {
            var q = Query.ReadOnly("BaseTable", Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = GetTableGrammar(new TableSchema("MyTable", q));
            var expected = new SqlExpression("SELECT [Id],[Name] INTO [MyTable] FROM [BaseTable] WHERE [Id] > 50");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateBasedTempTable()
        {
            var q = Query.ReadOnly("BaseTable", Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = GetTableGrammar(new TableSchema("MyTable", q) { Temporary = true });
            var expected = new SqlExpression("SELECT [Id],[Name] INTO [#MyTable] FROM [BaseTable] WHERE [Id] > 50");

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
            var expected = new SqlExpression("CREATE TABLE [MyTable] ([Id] INT IDENTITY(1,1) NOT NULL,[Name] VARCHAR(MAX) NULL,[Status] INT NULL,[Status2] INT NULL,CONSTRAINT [UC_MyTable_Status_Status2] UNIQUE ([Status],[Status2]),CONSTRAINT [PK_MyTable] PRIMARY KEY ([Id]))");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.AddPk("Id2");

            var grammar = GetTableGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE [MyTable] ([Id] INT IDENTITY(1,1) NOT NULL,[Id2] INT NOT NULL,CONSTRAINT [PK_MyTable] PRIMARY KEY ([Id],[Id2]))");

            Assert.Equal(expected, grammar.Create());
        }
    }
}
