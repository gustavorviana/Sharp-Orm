using QueryTest.Fixtures;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Mysql
{
    public class TableBuilderTest(ITestOutputHelper output, DbFixture<MysqlQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<DbFixture<MysqlQueryConfig>>, ITableBuilderTest
    {
        [Fact]
        public void ExistsTableTest()
        {
            var grammar = this.GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("CALL sys.table_exists(DATABASE(),\"MyTable\",@`table_type`);SELECT @`table_type`=\"BASE TABLE\";");

            Assert.Equal(expected, grammar.Exists());
        }

        [Fact]
        public void ExistsTempTableTest()
        {
            var grammar = this.GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("CALL sys.table_exists(DATABASE(),\"MyTable\",@`table_type`);SELECT @`table_type`=\"TEMPORARY\";");
            var current = grammar.Exists();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void DropTableTest()
        {
            var grammar = this.GetTableGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE `MyTable`");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void DropTempTableTest()
        {
            var grammar = this.GetTableGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TEMPORARY TABLE `MyTable`");
            var current = grammar.Drop();

            Assert.Equal(expected, current);
        }

        [Fact]
        public void CreateBasedTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetTableGrammar(new TableSchema("MyTable", q));
            var expected = new SqlExpression("CREATE TABLE `MyTable` SELECT `Id`, `Name` FROM `BaseTable` WHERE `Id` > 50");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateBasedTempTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetTableGrammar(new TableSchema("MyTable", q) { Temporary = true });
            var expected = new SqlExpression("CREATE TEMPORARY TABLE `MyTable` SELECT `Id`, `Name` FROM `BaseTable` WHERE `Id` > 50");

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

            var grammar = this.GetTableGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE `MyTable` (`Id` INT NOT NULL AUTO_INCREMENT,`Name` TEXT DEFAULT NULL,`Status` INT DEFAULT NULL,`Status2` INT DEFAULT NULL,CONSTRAINT `UC_MyTable` UNIQUE (`Status`,`Status2`),CONSTRAINT `PK_MyTable` PRIMARY KEY (`Id`))");

            Assert.Equal(expected, grammar.Create());
        }

        [Fact]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.AddPk("Id2");

            var grammar = this.GetTableGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE `MyTable` (`Id` INT NOT NULL AUTO_INCREMENT,`Id2` INT NOT NULL,CONSTRAINT `PK_MyTable` PRIMARY KEY (`Id`,`Id2`))");

            Assert.Equal(expected, grammar.Create());
        }
    }
}
