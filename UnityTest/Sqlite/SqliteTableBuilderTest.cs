using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using UnityTest.Utils;

namespace UnityTest.Sqlite
{
    [TestClass]
    public class SqliteTableBuilderTest : SqliteTest
    {
        [TestMethod]
        public void ExistsTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = \"MyTable\";");

            TestAssert.AreEqual(expected, grammar.Exists());
        }

        [TestMethod]
        public void ExistsTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("SELECT COUNT(*) FROM sqlite_temp_master WHERE type='table' AND name = \"temp_MyTable\";");

            TestAssert.AreEqual(expected, grammar.Exists());
        }

        [TestMethod]
        public void DropTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE \"MyTable\"");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void DropTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TABLE \"temp_MyTable\"");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void CreateBasedTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetGrammar(new TableSchema("MyTable", q));
            var expected = new SqlExpression("CREATE TABLE \"MyTable\" AS SELECT \"Id\", \"Name\" FROM \"BaseTable\" WHERE \"Id\" > 50");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        [TestMethod]
        public void CreateBasedTempTable()
        {
            var q = Query.ReadOnly("BaseTable", this.Config).Select("Id", "Name");
            q.Where("Id", ">", 50);

            var grammar = this.GetGrammar(new TableSchema("MyTable", q) { Temporary = true });
            var expected = new SqlExpression("CREATE TABLE temp.\"temp_MyTable\" AS SELECT \"Id\", \"Name\" FROM \"BaseTable\" WHERE \"Id\" > 50");

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
            var expected = new SqlExpression("CREATE TABLE \"MyTable\"(\"Id\" INTEGER NOT NULL,\"Name\" TEXT NULL,\"Status\" INTEGER NULL,\"Status2\" INTEGER NULL,CONSTRAINT \"UC_MyTable\" UNIQUE (\"Status\",\"Status2\"),PRIMARY KEY (\"Id\" AUTOINCREMENT))");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        [TestMethod]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.AddPk("Id2");

            var grammar = this.GetGrammar(new TableSchema("MyTable", cols));
            var expected = new SqlExpression("CREATE TABLE \"MyTable\"(\"Id\" INTEGER NOT NULL,\"Id2\" INTEGER NOT NULL,PRIMARY KEY (\"Id2\",\"Id\" AUTOINCREMENT))");

            TestAssert.AreEqual(expected, grammar.Create());
        }

        private SqliteTableGrammar GetGrammar(TableSchema schema)
        {
            return new SqliteTableGrammar(this.Config, schema);
        }
    }
}
