using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Data.Common;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class MysqlSelectBuilderTest : MysqlTableTest
    {
        [TestMethod]
        public void BasicSelect()
        {
            using var query = NewQuery();
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectCase()
        {
            using var query = NewQuery();
            using var g = new MysqlGrammar(query);
            query.Select(new Case(null, "Col").WhenNull("Column", "No value").When("Column2", 2, null).Else((Column)"Column + ' ' + Column2"));

            using var cmd = g.Select();
            Assert.AreEqual("SELECT CASE WHEN `Column` IS NULL THEN @v1 WHEN `Column2` = @v2 THEN @v3 ELSE Column + ' ' + Column2 END AS `Col` FROM `TestTable`", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@v1", "No value");
            AreEqualsParameter(cmd.Parameters[1], "@v2", 2);
            AreEqualsParameter(cmd.Parameters[2], "@v3", null);
        }

        [TestMethod]
        public void SelectCase2()
        {
            using var query = NewQuery();
            using var g = new MysqlGrammar(query);
            query.Select(new Case(null, "Col").When((Column)"`Column` IS NOT NULL", 1).Else((Column)"`Column` + ' ' + `Column2`"));

            using var cmd = g.Select();
            Assert.AreEqual("SELECT CASE WHEN `Column` IS NOT NULL THEN @v1 ELSE `Column` + ' ' + `Column2` END AS `Col` FROM `TestTable`", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@v1", 1);
        }

        [TestMethod]
        public void WhereBetween()
        {
            DateTime now = DateTime.Now;
            DateTime past = now.AddDays(-1);
            using var query = NewQuery();
            query.OrWhereNotBetween("Date", past, now);
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `Date` NOT BETWEEN @c1 AND @c2", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", past);
            AreEqualsParameter(cmd.Parameters[1], "@c2", now);
        }

        [TestMethod]
        public void WhereIn()
        {
            using var query = NewQuery();
            query.WhereIn("Status", 1, 2, 3).OrWhereIn("Status2", "");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `Status` IN (1, 2, 3) OR `Status2` IN (@c1)", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "");
        }

        [TestMethod]
        public void WhereNotIn()
        {
            using var query = NewQuery();
            query.WhereNotIn("Status", 1, 2, 3).OrWhereNotIn("Status2", 3, 4, 5);
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `Status` NOT IN (1, 2, 3) OR `Status2` NOT IN (3, 4, 5)", cmd.CommandText);
        }

        [TestMethod]
        public void WhereInColumn()
        {
            using var query = NewQuery();
            query.WhereInColumn(1, "Status", "Status2").OrWhereInColumn(4, "Status3", "Status4");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE 1 IN (`Status`,`Status2`) OR 4 IN (`Status3`,`Status4`)", cmd.CommandText);
        }

        [TestMethod]
        public void WhereNotInColumn()
        {
            using var query = NewQuery();
            query.WhereNotInColumn(1, "Status", "Status2").OrWhereNotInColumn(4, "Status3", "Status4");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE 1 NOT IN (`Status`,`Status2`) OR 4 NOT IN (`Status3`,`Status4`)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereNull()
        {
            using var query = NewQuery();
            query.Where("Column", null).OrWhereNull("Column2");
            using var g = new MysqlGrammar(query);
            using var cmd = g.Select();

            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `Column` IS NULL OR `Column2` IS NULL", cmd.CommandText);
        }

        [TestMethod]
        public void SelectColumnsName()
        {
            using var query = NewQuery();
            query.Select("Id", "Name");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT `Id`, `Name` FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column("Name", "meuNome"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT `Id`, `Name` AS `meuNome` FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectRawColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column(new SqlExpression("TOLOWER(Name) AS meuNome")));
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT `Id`, TOLOWER(Name) AS meuNome FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithLimit()
        {
            using var query = new Query(Connection, TABLE) { Limit = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` LIMIT 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereIn()
        {
            using var query = new Query(Connection, TABLE);
            int[] list = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            query.Where("id", "IN", list);
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `id` IN (1, 2, 3, 4, 5, 6, 7, 8, 9)", cmd.CommandText);
        }

        private void TestListParameters(DbParameterCollection collection, int[] items)
        {
            for (int i = 0; i < items.Length; i++)
                AreEqualsParameter(collection[i], $"@c{i + 1}", items[i]);
        }

        [TestMethod]
        public void SelectWhereInQuery()
        {
            using var query = NewQuery();
            query.Where("id", "IN", CreateQueryForWhere());
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `id` IN (SELECT `Id` FROM `TestIds` WHERE `Type` = @c1)", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "Unity");
        }

        private static Query CreateQueryForWhere()
        {
            return (Query)new Query(Connection, "TestIds").Select("Id").Where("Type", "=", "Unity");
        }

        [TestMethod]
        public void SelectWithOffset()
        {
            using var query = new Query(Connection, TABLE) { Offset = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimit()
        {
            using var query = new Query(Connection, TABLE) { Offset = 10, Limit = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` LIMIT 10 OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithDistinct()
        {
            using var query = new Query(Connection, TABLE) { Distinct = true };
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT DISTINCT * FROM `TestTable`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimitDistinct()
        {
            using var query = new Query(Connection, TABLE) { Offset = 10, Limit = 10, Distinct = true };
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT DISTINCT * FROM `TestTable` LIMIT 10 OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectBasicWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column` = @c1", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "value");
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = new Query(Connection, TABLE) { Limit = 10 };
            query.Where("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column` = @c1 LIMIT 10", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "value");
        }

        [TestMethod]
        public void SelectWhereCallbackQuery()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(e => e.Where("column", "=", "value"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE (`column` = @c1)", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "value");
        }

        [TestMethod]
        public void SelectMultipleWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", "value1");
            query.Where(e => e.Where("column2", "=", "value2"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column1` = @c1 AND (`column2` = @c2)", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "value1");
            AreEqualsParameter(cmd.Parameters[1], "@c2", "value2");
        }

        [TestMethod]
        public void SelectWhereOr()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column", "=", "teste")
                .OrWhere("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column` = @c1 OR `column` = @c2", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "teste");
            AreEqualsParameter(cmd.Parameters[1], "@c2", "value");
        }

        [TestMethod]
        public void SelectWhereColumnsEquals()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", new Column("column2"))
                .Where(new Column("column2"), "=", new Column("column3"));

            using var g = new MysqlGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column1` = `column2` AND `column2` = `column3`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereSqlExpression()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(new SqlExpression("column1 = 1"));

            using var g = new MysqlGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE column1 = 1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereRawColumn()
        {
            using var query = new Query(Connection, TABLE);
            query.Where((Column)"UPPER(column1)", "=", "ABC");

            using var g = new MysqlGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE UPPER(column1) = @c1", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@c1", "ABC");
        }

        [TestMethod]
        public void SelectWhereRawValue()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", (SqlExpression)"UPPER(column2)");

            using var g = new MysqlGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` WHERE `column1` = UPPER(column2)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectInnerJoin()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2", "TAB2.id", "=", $"{TABLE}.idTab2");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` INNER JOIN `TAB2` ON `TAB2`.`id` = `TestTable`.`idTab2`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectLeftJoin()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2 tab2", "tab2.id", "=", $"{TABLE}.idTab2", "LEFT");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` LEFT JOIN `TAB2` `tab2` ON `tab2`.`id` = `TestTable`.`idTab2`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectJoinWithWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2", q => q.WhereColumn("TAB2.id", "=", $"{TABLE}.idTab2").OrWhereColumn("TAB2.id", "=", $"{TABLE}.idTab3"), "LEFT");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` LEFT JOIN `TAB2` ON `TAB2`.`id` = `TestTable`.`idTab2` OR `TAB2`.`id` = `TestTable`.`idTab3`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectGroupByColumnName()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy("Col1", "Col2");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` GROUP BY `Col1`, `Col2`", cmd.CommandText);
        }

        [TestMethod]
        public void SelectGroupByColumnObj()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy(new Column("Col1"), new Column(new SqlExpression("LOWER(Col2)")));
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` GROUP BY `Col1`, LOWER(Col2)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectOrderBy()
        {
            using var query = NewQuery(TABLE, "t").OrderBy("t.Name");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` `t` ORDER BY `t`.`Name` Asc", cmd.CommandText);
        }

        [TestMethod]
        public void SelectOrderByWithAlias()
        {
            using var query = NewQuery().OrderBy("Name");
            using var g = new MysqlGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM `TestTable` ORDER BY `Name` Asc", cmd.CommandText);
        }

        [TestMethod]
        public void ColumnCase()
        {
            const string SQL = "CASE `Column` WHEN ? THEN ? WHEN ? THEN ? END";
            using var query = NewQuery().OrderBy("Name");

            var c = new Case("Column", "Alias").When(1, "Yes").When(0, "No");

            Assert.AreEqual(SQL, c.ToExpression(query.Info.ToReadOnly(), false).ToString());
            Assert.AreEqual($"{SQL} AS `Alias`", c.ToExpression(query.Info.ToReadOnly()).ToString());
        }

        [TestMethod]
        public void CaseEmptyCase()
        {
            using var query = NewQuery().OrderBy("Name");
            Assert.ThrowsException<InvalidOperationException>(() => new Case().ToExpression(query.Info.ToReadOnly()));
        }

        [TestMethod]
        public void ColumnCaseExpression()
        {
            const string SQL = "CASE WHEN `Column` >= ? THEN ? WHEN `Column` BETWEEN 11 AND 12 THEN ? END";
            using var query = NewQuery().OrderBy("Name");

            var c = new Case().When("Column", ">=", "10", "No").When((SqlExpression)"`Column` BETWEEN 11 AND 12", "InRange");
            var exp = c.ToExpression(query.Info.ToReadOnly(), false);
            Assert.AreEqual(SQL, exp.ToString());
            Assert.AreEqual("10", exp.Parameters[0]);
            Assert.AreEqual("No", exp.Parameters[1]);
            Assert.AreEqual("InRange", exp.Parameters[2]);
        }

        private static void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            if (value == null || value is DBNull) Assert.IsTrue(param.Value is DBNull);
            else Assert.AreEqual(value, param.Value);
        }
    }
}
