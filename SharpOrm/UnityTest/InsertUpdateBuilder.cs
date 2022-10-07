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
            var command = Grammar.InsertCommand(q, this.NewRow(1, "T1").Cells);
            Assert.AreEqual("INSERT INTO TestTable (id, name) VALUES (@p0, @p1)", command.CommandText);
            Assert.AreEqual(2, command.Parameters.Count);
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            var row = new Row[] { this.NewRow(1, "T1"), this.NewRow(2, "T2"), this.NewRow(3, "T3"), this.NewRow(4, "T4"), this.NewRow(5, "T5") };

            var command = Grammar.BulkInsertCommand(q, row);
            Assert.AreEqual("INSERT INTO TestTable (id, name) VALUES (@p0, @p1), (@p2, @p3), (@p4, @p5), (@p6, @p7), (@p8, @p9)", command.CommandText);
            Assert.AreEqual(10, command.Parameters.Count);
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            q.Where("id", "=", 1);

            var command = Grammar.UpdateCommand(q, new Cell[] { new Cell("name", 2) });
            Assert.AreEqual("UPDATE TestTable SET name = @p1 WHERE id = @p0", command.CommandText);
            Assert.AreEqual(2, command.Parameters.Count);
        }

        private Row NewRow(int id, string name)
        {
            return new Row(new Cell[] { new Cell("id", id), new Cell("name", name) });
        }
    }
}
