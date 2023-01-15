using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System.Data.Common;
using System.Linq;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class MysqlInsertUpdateDeleteBuilderTest : MysqlTableTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetInsertCommand(NewRow(1, "T1").Cells);
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`) VALUES (@v1, @v2); SELECT LAST_INSERT_ID();", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@v1", 1);
            AreEqualsParameter(cmd.Parameters[1], "@v2", "T1");
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            using var cmd = g.GetBulkInsertCommand(rows);
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`) VALUES (@v1, @v2), (@v3, @v4), (@v5, @v6), (@v7, @v8), (@v9, @v10)", cmd.CommandText);

            this.TestBulkInsertParams(cmd, rows);
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            var row = new Row(new Cell[] { new Cell("name", "MyTestName"), new Cell("alias", "Test") });
            using var cmd = g.GetUpdateCommand(row.Cells);
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @v1, `alias` = @v2", cmd.CommandText);

            this.AreEqualsParameter(cmd.Parameters[0], "@v1", row[0].Value);
            this.AreEqualsParameter(cmd.Parameters[1], "@v2", row[1].Value);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            using var cmd = g.GetUpdateCommand(new Cell[] { new Cell("name", 2) });
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @v1 WHERE `id` = @c1", cmd.CommandText);

            this.AreEqualsParameter(cmd.Parameters[0], "@v1", 2);
            this.AreEqualsParameter(cmd.Parameters[1], "@c1", 1);
        }

        [TestMethod]
        public void Delete()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetDeleteCommand();
            Assert.AreEqual("DELETE FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteWhere()
        {
            using var q = NewQuery();
            q.Where("id", "=", 1);
            using var g = new MysqlGrammar(q);

            using var cmd = g.GetDeleteCommand();
            Assert.AreEqual("DELETE FROM `TestTable` WHERE `id` = @c1", cmd.CommandText);

            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        private void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.AreEqual(value, param.Value);
        }

        private void TestBulkInsertParams(DbCommand command, Row[] rows)
        {
            var dbParams = command.Parameters.OfType<DbParameter>().Where(p => p.ParameterName.StartsWith("@v")).ToArray();

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                this.AreEqualsParameter(dbParams[i * 2], $"@v{(i * 2) + 1}", row[ID]);
                this.AreEqualsParameter(dbParams[(i * 2) + 1], $"@v{(i * 2) + 2}", row[NAME]);
            }
        }
    }
}
