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

            q.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            using var cmd = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`, `value`) VALUES (1, @p1, NULL); SELECT LAST_INSERT_ID();", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@p1", "T1");
        }

        [TestMethod]
        public void InsertWIthRaw()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.Insert(new Cell[] { new Cell(ID, (SqlExpression)"1") });
            Assert.AreEqual("INSERT INTO `TestTable` (`id`) VALUES (1); SELECT LAST_INSERT_ID();", cmd.CommandText);
        }

        [TestMethod]
        public void InsertExtendedClass()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var table = new ExtendedTestTable
            {
                Id = 1,
                CreatedAt = DateTime.Now,
                CustomId = Guid.NewGuid(),
                Name = "Name",
                Number = 2.1M,
                ExtendedProp = "Nothing",
                CustomStatus = Status.Success,
                Nick = null
            };

            using var cmd = g.Insert(Row.Parse(table, typeof(TestTable), true, false).Cells);
            Assert.AreEqual("INSERT INTO `TestTable` (`Id`, `Name`, `Nick`, `record_created`, `Number`, `custom_id`, `custom_status`) VALUES (1, @p1, NULL, @p2, 2.1, @p3, 1); SELECT LAST_INSERT_ID();", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@p1", table.Name);
            AreEqualsParameter(cmd.Parameters[1], "@p2", table.CreatedAt);
            AreEqualsParameter(cmd.Parameters[2], "@p3", table.CustomId.ToString());
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            using var cmd = g.BulkInsert(rows);
            Assert.AreEqual("INSERT INTO `TestTable` (`id`, `name`) VALUES (1, @p1), (2, @p2), (3, @p3), (4, @p4), (5, @p5)", cmd.CommandText);

            TestBulkInsertParams(cmd, rows);
        }

        private static new Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name));
        }

        [TestMethod]
        public void UpdateNoColumns()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            Assert.ThrowsException<InvalidOperationException>(() => g.Update(Array.Empty<Cell>()));
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            using var cmd = g.Update(row.Cells);
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @p1, `alias` = @p2, `value` = NULL, `status` = 1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@p1", row[0].Value);
            AreEqualsParameter(cmd.Parameters[1], "@p2", row[1].Value);
        }

        [TestMethod]
        public void UpdateCaseValue()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            const string CaseMsg = "Without alias";
            const string ElseMsg = "With alias";

            var caseVal = new Case().When("alias", "IS", null, CaseMsg).Else(ElseMsg);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", caseVal), new Cell("value", null), new Cell("status", Status.Success));
            using var cmd = g.Update(row.Cells);
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @p1, `alias` = CASE WHEN `alias` IS NULL THEN @p2 ELSE @p3 END, `value` = NULL, `status` = 1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@p1", row[0].Value);
            AreEqualsParameter(cmd.Parameters[1], "@p2", CaseMsg);
            AreEqualsParameter(cmd.Parameters[2], "@p3", ElseMsg);
        }

        [TestMethod]
        public void UpdateByColumn()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            using var cmd = g.Update(new Cell[] { new Cell("name", new Column("nick")) });
            Assert.AreEqual("UPDATE `TestTable` SET `name` = `nick` WHERE `id` = 1", cmd.CommandText);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery();
            using var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            using var cmd = g.Update(new Cell[] { new Cell("name", "MyName") });
            Assert.AreEqual("UPDATE `TestTable` SET `name` = @p1 WHERE `id` = 1", cmd.CommandText);

            AreEqualsParameter(cmd.Parameters[0], "@p1", "MyName");
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
        public void DeleteLimit()
        {
            using var q = NewQuery();
            q.Limit = 5;
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE FROM `TestTable` LIMIT 5", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteOrder()
        {
            using var q = NewQuery();
            q.OrderBy("id");
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE FROM `TestTable` ORDER BY `id` Asc", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteWhere()
        {
            using var q = NewQuery();
            q.Where("id", "=", 1);
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE FROM `TestTable` WHERE `id` = 1", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteWhereJoin()
        {
            using var q = NewQuery(TABLE, "t1");
            q.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE `t1` FROM `TestTable` `t1` INNER JOIN `Table2` `t2` ON `t2`.`Id` = `t1`.`T2Id` WHERE `t2`.`Id` = 1", cmd.CommandText);
        }

        [TestMethod]
        public void DeleteJoins()
        {
            using var q = NewQuery(TABLE, "t1");
            q.JoinToDelete("t2").Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            using var g = new MysqlGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE `t1`, `t2` FROM `TestTable` `t1` INNER JOIN `Table2` `t2` ON `t2`.`Id` = `t1`.`T2Id` WHERE `t2`.`Id` = 1", cmd.CommandText);
        }

        [TestMethod]
        public void InsertByBasicSelect()
        {
            using var selectQuery = NewQuery("User");
            selectQuery
                .Select(new Column("Id"), (Column)"1")
                .Where("id", 1);

            using var q = NewQuery();
            using var g = new MysqlGrammar(q);

            using var cmd = g.InsertQuery(selectQuery, new[] { "UserId", "Status" });
            Assert.AreEqual("INSERT INTO `TestTable` (`UserId`, `Status`) SELECT `Id`, 1 FROM `User` WHERE `id` = 1", cmd.CommandText);
        }

        private static void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            if (value == null || value is DBNull) Assert.IsTrue(param.Value is DBNull);
            else Assert.AreEqual(value, param.Value);
        }

        private static void TestBulkInsertParams(DbCommand command, Row[] rows)
        {
            var dbParams = command.Parameters.OfType<DbParameter>().Where(p => p.ParameterName.StartsWith("@p")).ToArray();

            for (int i = 0; i < rows.Length; i++)
            {
                AreEqualsParameter(dbParams[i], $"@p{i + 1}", rows[i][NAME]);
            }
        }
    }
}
