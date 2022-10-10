using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using Teste.Utils;

namespace UnityTest
{
    [TestClass]
    public class InsertUpdateBuilder : MysqlTableTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetInsertCommand(NewRow(1, "T1").Cells);
            Assert.AreEqual("INSERT INTO TestTable (id, name) VALUES (@v1, @v2)", cmd.CommandText);
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var row = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            using var cmd = g.GetBulkInsertCommand(row);
            Assert.AreEqual("INSERT INTO TestTable (id, name) VALUES (@v1, @v2), (@v3, @v4), (@v5, @v6), (@v7, @v8), (@v9, @v10)", cmd.CommandText);
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetUpdateCommand(new Cell[] { new Cell("name", "MyTestName"), new Cell("alias", "Test") });
            Assert.AreEqual("UPDATE TestTable SET name = @v1, alias = @v2", cmd.CommandText);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            using var cmd = g.GetUpdateCommand(new Cell[] { new Cell("name", 2) });
            Assert.AreEqual("UPDATE TestTable SET name = @v1 WHERE id = @c1", cmd.CommandText);
        }

        [TestMethod]
        public void InsertWithPrefixSuffix()
        {
            this.ApplyDefaultColumnPrefixSuffix();

            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetInsertCommand(NewRow(1, "T1").Cells);
            Assert.AreEqual("INSERT INTO TestTable (`id`, `name`) VALUES (@v1, @v2)", cmd.CommandText);
        }

        [TestMethod]
        public void BulkInsertWithPrefixSuffix()
        {
            this.ApplyDefaultColumnPrefixSuffix();

            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var row = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            using var cmd = g.GetBulkInsertCommand(row);
            Assert.AreEqual("INSERT INTO TestTable (`id`, `name`) VALUES (@v1, @v2), (@v3, @v4), (@v5, @v6), (@v7, @v8), (@v9, @v10)", cmd.CommandText);
        }

        [TestMethod]
        public void UpdateWithPrefixSuffix()
        {
            this.ApplyDefaultColumnPrefixSuffix();

            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetUpdateCommand(new Cell[] { new Cell("name", "MyTestName"), new Cell("alias", "Test") });
            Assert.AreEqual("UPDATE TestTable SET `name` = @v1, `alias` = @v2", cmd.CommandText);
        }

        private void ApplyDefaultColumnPrefixSuffix()
        {
            QueryDefaults.Config.ColumnPrefix = "`";
            QueryDefaults.Config.ColumnSuffix = "`";
        }
    }
}
