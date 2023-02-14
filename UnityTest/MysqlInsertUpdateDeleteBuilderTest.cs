using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Linq;
using UnityTest.Models;
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

            using var cmd = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`, `value`) VALUES (@v1, @v2, @v3); SELECT LAST_INSERT_ID();", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@v1", 1);
            AreEqualsParameter(cmd.Parameters[1], "@v2", "T1");
            IsDbNullParam(cmd.Parameters[2], "@v3");
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            using var cmd = g.BulkInsert(rows);
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`) VALUES (@v1, @v2), (@v3, @v4), (@v5, @v6), (@v7, @v8), (@v9, @v10)", cmd.CommandText);

            TestBulkInsertParams(cmd, rows);
        }

        private static new Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name));
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            using var cmd = g.Update(row.Cells);
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @v1, `alias` = @v2, `value` = @v3, `status` = @v4", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@v1", row[0].Value);
            AreEqualsParameter(cmd.Parameters[1], "@v2", row[1].Value);
            IsDbNullParam(cmd.Parameters[2], "@v3");
            AreEqualsParameter(cmd.Parameters[3], "@v4", (int)Status.Success);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            using var cmd = g.Update(new Cell[] { new Cell("name", 2) });
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @v1 WHERE `id` = @c1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@v1", 2);
            AreEqualsParameter(cmd.Parameters[1], "@c1", 1);
        }

        [TestMethod]
        public void Delete()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteWhere()
        {
            using var q = NewQuery();
            q.Where("id", "=", 1);
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE FROM `TestTable` WHERE `id` = @c1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        [TestMethod]
        public void InsertByBasicSelect()
        {
            using var selectQuery = new Query("User");
            selectQuery
                .Select(new Column("Id"), (Column)"1")
                .Where("id", 1);

            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.InsertQuery(selectQuery, new[] { "UserId", "Status" });
            Assert.AreEqual("INSERT INTO `TestTable` (UserId,Status) SELECT `Id`, 1 FROM `User` WHERE `id` = @c1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        private static void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.AreEqual(value, param.Value);
        }

        private static void IsDbNullParam(DbParameter param, string name)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.IsTrue(param.Value is DBNull);
        }

        private static void TestBulkInsertParams(DbCommand command, Row[] rows)
        {
            var dbParams = command.Parameters.OfType<DbParameter>().Where(p => p.ParameterName.StartsWith("@v")).ToArray();

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                AreEqualsParameter(dbParams[i * 2], $"@v{(i * 2) + 1}", row[ID]);
                AreEqualsParameter(dbParams[(i * 2) + 1], $"@v{(i * 2) + 2}", row[NAME]);
            }
        }
    }
}
