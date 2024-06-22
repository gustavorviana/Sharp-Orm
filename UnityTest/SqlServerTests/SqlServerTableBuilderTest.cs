using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerTableBuilderTest : SqlServerTest
    {
        [TestMethod]
        public void ExistsTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ?;", grammar.Name.Name);

            TestAssert.AreEqual(expected, grammar.Exists());
        }

        [TestMethod]
        public void ExistsTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE xtype = 'u' AND object_id('tempdb..' + name) IS NOT NULL AND LEFT(name,LEN(name) - PATINDEX('%[^_]%', REVERSE(LEFT(name, LEN(name) - 12))) - 11) = ?", grammar.Name.Name);
            var current = grammar.Exists();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void DropTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE [MyTable]");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void DropTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TABLE [#MyTable]");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void CreateBasedTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetGrammar(new TableSchema("MyTable", q));
            var expected = new SqlExpression("SELECT [Id],[Name] INTO [MyTable] FROM [BaseTable] WHERE [Id] > 50");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        [TestMethod]
        public void CreateBasedTempTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetGrammar(new TableSchema("MyTable", q) { Temporary = true });
            var expected = new SqlExpression("SELECT [Id],[Name] INTO [#MyTable] FROM [BaseTable] WHERE [Id] > 50");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        [TestMethod]
        public void CreateTable()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.Add<string>("Name");
            cols.Add<int>("Status").Unique = true;
            cols.Add<int>("Status2").Unique = true;

            var grammar = this.GetGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE [MyTable] ([Id] INT IDENTITY(1,1) NOT NULL,[Name] VARCHAR(MAX) NULL,[Status] INT NULL,[Status2] INT NULL,CONSTRAINT [UC_MyTable] UNIQUE ([Status],[Status2]),CONSTRAINT [PK_MyTable] PRIMARY KEY ([Id]))");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        [TestMethod]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.AddPk("Id2");

            var grammar = this.GetGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE [MyTable] ([Id] INT IDENTITY(1,1) NOT NULL,[Id2] INT NOT NULL,CONSTRAINT [PK_MyTable] PRIMARY KEY ([Id],[Id2]))");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        private SqlServerTableGrammar GetGrammar(TableSchema schema)
        {
            return new SqlServerTableGrammar(this.Config, schema);
        }
    }
}
