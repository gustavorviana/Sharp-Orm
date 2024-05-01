using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class MysqlSelectBuilderTest : MysqlTableTest
    {
        [TestMethod]
        public void FixColumnName()
        {
            var config = new MysqlQueryConfig(false);
            string basic = config.ApplyNomenclature("colName");
            string withTable = config.ApplyNomenclature("table.colName");
            string all = config.ApplyNomenclature("*");
            string allWithTable = config.ApplyNomenclature("table.*");

            Assert.AreEqual("`colName`", basic);
            Assert.AreEqual("`table`.`colName`", withTable);
            Assert.AreEqual("*", all);
            Assert.AreEqual("`table`.*", allWithTable);
        }

        [TestMethod]
        public void BasicSelect()
        {
            using var query = NewQuery();
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void Basic2Select()
        {
            using var query = NewQuery("TestTable", "table").Select("table.*");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT `table`.* FROM `TestTable` `table`", sqlExpression);
        }

        [TestMethod]
        public void SelectCase()
        {
            using var query = NewQuery();
            var g = new MysqlGrammar(query);
            query.Select(new Case(null, "Col").WhenNull("Column", "No value").When("Column2", 2, null).Else((Column)"Column + ' ' + Column2"));

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT CASE WHEN `Column` IS NULL THEN @p1 WHEN `Column2` = 2 THEN NULL ELSE Column + ' ' + Column2 END AS `Col` FROM `TestTable`", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereBool()
        {
            using var query = NewQuery();
            query.Where("First", true).OrWhere("Left", false);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `First` = 1 OR `Left` = 0", sqlExpression);
        }

        [TestMethod]
        public void SelectCase2()
        {
            using var query = NewQuery();
            var g = new MysqlGrammar(query);
            query.Select(new Case(null, "Col").When((Column)"`Column` IS NOT NULL", 1).Else((Column)"`Column` + ' ' + `Column2`"));

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT CASE WHEN `Column` IS NOT NULL THEN 1 ELSE `Column` + ' ' + `Column2` END AS `Col` FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void WhereBetween()
        {
            using var query = NewQuery();
            query.WhereBetween("N", 1, 2).OrWhereBetween("N2", 3, 4);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` BETWEEN 1 AND 2 OR `N2` BETWEEN 3 AND 4", sqlExpression);
        }

        [TestMethod]
        public void WhereNotBetween()
        {
            using var query = NewQuery();
            query.WhereNotBetween("N", 1, 2).OrWhereNotBetween("N2", 3, 4);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` NOT BETWEEN 1 AND 2 OR `N2` NOT BETWEEN 3 AND 4", sqlExpression);
        }

        [TestMethod]
        public void WhereExistsExpression()
        {
            using var query = NewQuery();
            query.Exists(new SqlExpression("1")).OrExists(new SqlExpression("?", "5"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE EXISTS (1) OR EXISTS (@p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereNotExistsExpression()
        {
            using var query = NewQuery();
            query.NotExists(new SqlExpression("1")).OrNotExists(new SqlExpression("?", "5"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE NOT EXISTS (1) OR NOT EXISTS (@p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereExistsQuery()
        {
            using var qTest = NewQuery("Test").Select("Col");
            qTest.Where("Col", ">", 1);

            using var qTest2 = NewQuery("Test").Select("Col");
            qTest2.Where("Col1", "=", "2");

            using var query = NewQuery();
            query.Exists(qTest).OrExists(qTest2);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE EXISTS (SELECT `Col` FROM `Test` WHERE `Col` > 1) OR EXISTS (SELECT `Col` FROM `Test` WHERE `Col1` = @p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereNotExistsQuery()
        {
            using var qTest = NewQuery("Test").Select("Col");
            qTest.Where("Col", ">", 1);

            using var qTest2 = NewQuery("Test").Select("Col");
            qTest2.Where("Col1", "=", "2");

            using var query = NewQuery();
            query.NotExists(qTest).OrNotExists(qTest2);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE NOT EXISTS (SELECT `Col` FROM `Test` WHERE `Col` > 1) OR NOT EXISTS (SELECT `Col` FROM `Test` WHERE `Col1` = @p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereIn()
        {
            using var query = NewQuery();
            query.WhereIn("Status", 1, 2, 3).OrWhereIn("Status2", "");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Status` IN (1, 2, 3) OR `Status2` IN (@p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void WhereInEmpty()
        {
            using var query = NewQuery();
            query.WhereIn("Status", Array.Empty<int>());
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE 1!=1", sqlExpression);
        }

        [TestMethod]
        public void WhereNotIn()
        {
            using var query = NewQuery();
            query.WhereNotIn("Status", 1, 2, 3).OrWhereNotIn("Status2", 3, 4, 5);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Status` NOT IN (1, 2, 3) OR `Status2` NOT IN (3, 4, 5)", sqlExpression);
        }

        [TestMethod]
        public void WhereInColumn()
        {
            using var query = NewQuery();
            query.WhereInColumn(1, "Status", "Status2").OrWhereInColumn(4, "Status3", "Status4");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE 1 IN (`Status`, `Status2`) OR 4 IN (`Status3`, `Status4`)", sqlExpression);
        }

        [TestMethod]
        public void WhereInExpression()
        {
            using var query = NewQuery();
            query.WhereIn("N", new SqlExpression("1, ?", "2")).OrWhereIn("N2", new SqlExpression("3, ?", "4"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` IN (1, @p1) OR `N2` IN (3, @p2)", sqlExpression);
        }

        [TestMethod]
        public void WhereNotInExpression()
        {
            using var query = NewQuery();
            query.WhereNotIn("N", new SqlExpression("1, ?", "2")).OrWhereNotIn("N2", new SqlExpression("3, ?", "4"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` NOT IN (1, @p1) OR `N2` NOT IN (3, @p2)", sqlExpression);
        }

        [TestMethod]
        public void WhereInList()
        {
            using var query = NewQuery();
            query.WhereIn<int>("N", new List<int> { 1, 2 }).OrWhereIn<string>("N2", new List<string> { "3", "4" });
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` IN (1, 2) OR `N2` IN (@p1, @p2)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void WhereNotInList()
        {
            using var query = NewQuery();
            query.WhereNotIn<int>("N", new List<int> { 1, 2 }).OrWhereNotIn<string>("N2", new List<string> { "3", "4" });
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `N` NOT IN (1, 2) OR `N2` NOT IN (@p1, @p2)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void WhereNotInColumn()
        {
            using var query = NewQuery();
            query.WhereNotInColumn(1, "Status", "Status2").OrWhereNotInColumn(4, "Status3", "Status4");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE 1 NOT IN (`Status`, `Status2`) OR 4 NOT IN (`Status3`, `Status4`)", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereNot()
        {
            using var query = NewQuery();
            query.WhereNot("Column", 0).OrWhereNot("Column2", "Text");
            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();

            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Column` != 0 OR `Column2` != @p1", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereNotNull()
        {
            using var query = NewQuery();
            query.WhereNotNull("Column").OrWhereNotNull("Column2");
            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();

            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Column` IS NOT NULL OR `Column2` IS NOT NULL", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereNull()
        {
            using var query = NewQuery();
            query.Where("Column", null).WhereNull("Column3").OrWhereNull("Column2");
            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();

            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Column` IS NULL AND `Column3` IS NULL OR `Column2` IS NULL", sqlExpression);
        }

        [TestMethod]
        public void SelectExpression()
        {
            using var query = NewQuery();
            query.Where(new SqlExpression("`Column` IS NULL"));
            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();

            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Column` IS NULL", sqlExpression);
        }

        [TestMethod]
        public void SelectExpressionOr()
        {
            using var query = NewQuery();
            query.Where(new SqlExpression("`Column` IS NULL")).OrWhere(new SqlExpression("`Column2` IS NULL"));
            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();

            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Column` IS NULL OR `Column2` IS NULL", sqlExpression);
        }

        [TestMethod]
        public void SelectColumnsName()
        {
            using var query = NewQuery();
            query.Select("Id", "Name");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT `Id`, `Name` FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void SelectColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column("Name", "meuNome"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT `Id`, `Name` AS `meuNome` FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void SelectRawColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column(new SqlExpression("TOLOWER(Name) AS meuNome")));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT `Id`, TOLOWER(Name) AS meuNome FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void SelectWithLimit()
        {
            using var query = new Query(Connection, TABLE) { Limit = 10 };
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` LIMIT 10", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereIn()
        {
            using var query = new Query(Connection, TABLE);
            int[] list = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            query.Where("id", "IN", list);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `id` IN (1, 2, 3, 4, 5, 6, 7, 8, 9)", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereInQuery()
        {
            using var query = NewQuery();
            query.Where("id", "IN", CreateQueryForWhere());
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `id` IN (SELECT `Id` FROM `TestIds` WHERE `Type` = @p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        private static Query CreateQueryForWhere()
        {
            return (Query)new Query(Connection, "TestIds").Select("Id").Where("Type", "=", "Unity");
        }

        [TestMethod]
        public void SelectWithOffsetLimit()
        {
            using var query = new Query(Connection, TABLE) { Offset = 10, Limit = 10 };
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` LIMIT 10 OFFSET 10", sqlExpression);
        }

        [TestMethod]
        public void SelectWithDistinct()
        {
            using var query = new Query(Connection, TABLE) { Distinct = true };
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT DISTINCT * FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void SelectWithOffsetLimitDistinct()
        {
            using var query = new Query(Connection, TABLE) { Offset = 10, Limit = 10, Distinct = true };
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT DISTINCT * FROM `TestTable` LIMIT 10 OFFSET 10", sqlExpression);
        }

        [TestMethod]
        public void SelectBasicWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column", "=", "value");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column` = @p1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = new Query(Connection, TABLE) { Limit = 10 };
            query.Where("column", "=", "value");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column` = @p1 LIMIT 10", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void SelectWhereCallbackQuery()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(e => e.Where("column", "=", "value"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE (`column` = @p1)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void SelectMultipleWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", "value1");
            query.Where(e => e.Where("column2", "=", "value2"));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column1` = @p1 AND (`column2` = @p2)", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void SelectWhereOr()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column", "=", "teste")
                .OrWhere("column", "=", "value");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column` = @p1 OR `column` = @p2", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void SelectWhereColumnsEquals()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", new Column("column2"))
                .Where(new Column("column2"), "=", new Column("column3"));

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column1` = `column2` AND `column2` = `column3`", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereSqlExpression()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(new SqlExpression("column1 = 1"));

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE column1 = 1", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereStartsWith()
        {
            using var query = new Query(Connection, TABLE);
            query.WhereStartsWith("Name", "Rod").OrWhereStartsWith("Name", "Mar");

            var g = new MysqlGrammar(query);
            TestAssert.TestExpected(
                g.Select(),
                "SELECT * FROM `TestTable` WHERE `Name` LIKE @p1 OR `Name` LIKE @p2",
                new object[] {
                    "Rod%",
                    "Mar%"
                }
            );
        }

        [TestMethod]
        public void SelectWhereContains()
        {
            using var query = new Query(Connection, TABLE);
            query.WhereContains("Title", "10%").OrWhereContains("Title", "pixel");

            var g = new MysqlGrammar(query);
            TestAssert.TestExpected(
                g.Select(),
                "SELECT * FROM `TestTable` WHERE `Title` LIKE @p1 OR `Title` LIKE @p2",
                new object[] {
                    "%10\\%%",
                    "%pixel%"
                }
            );
        }

        [TestMethod]
        public void SelectWhereEndsWith()
        {
            using var query = new Query(Connection, TABLE);
            query.WhereEndsWith("Title", "30%").OrWhereEndsWith("Title", "80%");

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.TestExpected(
                g.Select(),
                "SELECT * FROM `TestTable` WHERE `Title` LIKE @p1 OR `Title` LIKE @p2",
                new object[] {
                    "%30\\%",
                    "%80\\%"
                }
            );
        }

        [TestMethod]
        public void SelectWhereRawColumn()
        {
            using var query = new Query(Connection, TABLE);
            query.Where((Column)"UPPER(column1)", "=", "ABC");

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE UPPER(column1) = @p1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void SelectWhereRawValue()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("column1", "=", (SqlExpression)"UPPER(column2)");

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `column1` = UPPER(column2)", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereCallback()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(q => q.Where("C1", 1).Where("C2", 2)).OrWhere(q => q.Where("C3", 3).Where("C4", 5));

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE (`C1` = 1 AND `C2` = 2) OR (`C3` = 3 AND `C4` = 5)", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereSubCallback()
        {
            using var query = new Query(Connection, TABLE);
            query.Where(q => q.Where("C1", 1).Where("C2", 2).Where(q => q.Where("C3", 3).Where("C4", 5)));

            var g = new MysqlGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE (`C1` = 1 AND `C2` = 2 AND (`C3` = 3 AND `C4` = 5))", sqlExpression);
        }

        [TestMethod]
        public void SelectInnerJoin()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2", "TAB2.id", "=", $"{TABLE}.idTab2");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` INNER JOIN `TAB2` ON `TAB2`.`id` = `TestTable`.`idTab2`", sqlExpression);
        }

        [TestMethod]
        public void SelectLeftJoin()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2 tab2", "tab2.id", "=", $"{TABLE}.idTab2", "LEFT");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` LEFT JOIN `TAB2` `tab2` ON `tab2`.`id` = `TestTable`.`idTab2`", sqlExpression);
        }

        [TestMethod]
        public void SelectJoinWithWhere()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("TAB2", q => q.WhereColumn("TAB2.id", "=", $"{TABLE}.idTab2").OrWhereColumn("TAB2.id", "=", $"{TABLE}.idTab3"), "LEFT");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` LEFT JOIN `TAB2` ON `TAB2`.`id` = `TestTable`.`idTab2` OR `TAB2`.`id` = `TestTable`.`idTab3`", sqlExpression);
        }

        [TestMethod]
        public void SelectGroupByColumnName()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy("Col1", "Col2");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` GROUP BY `Col1`, `Col2`", sqlExpression);
        }

        [TestMethod]
        public void SelectHavingColumn()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy("Col1", "Col2").Having(q => q.Where("Col1", true));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` GROUP BY `Col1`, `Col2` HAVING `Col1` = 1", sqlExpression);
        }

        [TestMethod]
        public void SelectGroupByColumnObj()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy(new Column("Col1"), new Column(new SqlExpression("LOWER(Col2)")));
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` GROUP BY `Col1`, LOWER(Col2)", sqlExpression);
        }

        [TestMethod]
        public void SelectOrderBy()
        {
            using var query = NewQuery(TABLE, "t").OrderBy("t.Name");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` `t` ORDER BY `t`.`Name` Asc", sqlExpression);
        }

        [TestMethod]
        public void SelectOrderByWithAlias()
        {
            using var query = NewQuery().OrderBy("Name");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` ORDER BY `Name` Asc", sqlExpression);
        }

        [TestMethod]
        public void ColumnCase()
        {
            const string SQL = "CASE `Column` WHEN 1 THEN ? WHEN 0 THEN ? END";
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

        [TestMethod]
        public void BasicSqlExpressionSelect()
        {
            const string Name = "Test";

            TestAssert.TestExpectedSelectExpression(
                NewQuery(),
                "SELECT * FROM `TestTable` WHERE `Name` = @p1 AND `Active` = 1",
                new SqlExpression("`Name` = ? AND `Active` = ?", Name, true),
                new object[] { Name }
            );
        }

        [TestMethod]
        public void NonDecimalSqlExpressionSelect()
        {
            var exp = new SqlExpression(
                "`Int` = ? AND `Long` = ? AND `Byte` = ? AND `Sbyte` = ? AND `Short` = ? AND `Ushort` = ? AND `Uint` = ? AND `Ulong` = ?",
                1, 2L, (byte)3, (sbyte)4, (short)5, (ushort)6, 7u, (ulong)8
            );
            TestAssert.TestExpectedSelectExpression(
                NewQuery(),
                "SELECT * FROM `TestTable` WHERE `Int` = 1 AND `Long` = 2 AND `Byte` = 3 AND `Sbyte` = 4 AND `Short` = 5 AND `Ushort` = 6 AND `Uint` = 7 AND `Ulong` = 8",
                exp,
                Array.Empty<object>()
            );
        }

        [TestMethod]
        public void DecimalSqlExpressionSelect()
        {
            var exp = new SqlExpression(
                "`Float` = ? AND `Double` = ? AND `Decimal` = ?",
                1.1f, 2.2, 3.3m
            );
            TestAssert.TestExpectedSelectExpression(
                NewQuery(),
                "SELECT * FROM `TestTable` WHERE `Float` = 1.1 AND `Double` = 2.2 AND `Decimal` = 3.3",
                exp,
                Array.Empty<object>()
            );
        }

        [TestMethod]
        public void CountSelect()
        {
            using var query = new Query(Connection, TABLE);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void CountJoinSelect()
        {
            using var query = new Query(Connection, TABLE).Join("TestTable2 t2", "t2.Id", "TestTable.Id2").Select("TestTable.*");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM `TestTable` INNER JOIN `TestTable2` `t2` ON `t2`.`Id` = `TestTable`.`Id2`", sqlExpression);
        }

        [TestMethod]
        public void CountDistinctSelect()
        {
            using var query = new Query(Connection, TABLE);
            query.Distinct = true;
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM (SELECT DISTINCT * FROM `TestTable`) `count`", sqlExpression);
        }

        [TestMethod]
        public void CountWhereSelect()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("Column", null);
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM `TestTable` WHERE `Column` IS NULL", sqlExpression);
        }

        [TestMethod]
        public void CountDistinctSelect2()
        {
            using var query = new Query(Connection, TABLE);
            query.Select("Column").Distinct = true;
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(DISTINCT `Column`) FROM `TestTable`", sqlExpression);
        }

        [TestMethod]
        public void CountDistinctSelect3()
        {
            using var query = new Query(Connection, TABLE);
            query.Select("nick", "name").Distinct = true;
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM (SELECT DISTINCT `nick`, `name` FROM `TestTable`) `count`", sqlExpression);
        }

        [TestMethod]
        public void CountSelectJoin()
        {
            using var query = new Query(Connection, TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");
            var g = new MysqlGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM `TestTable` INNER JOIN `Table2` `t2` ON `t2`.`IdTable` = `TestTable`.`Id` WHERE `t2`.`Column` = @p1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void SelectHaving()
        {
            using var query = NewQuery();
            query.Select((Column)"Phone", (Column)"COUNT(Phone) AS 'PhonesCount'");
            query.GroupBy("Phone");
            query.Having(h => h.Where(new SqlExpression("COUNT(Phone) > 1")));
            query.OrderByDesc("PhonesCount");

            var g = new MysqlGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT Phone, COUNT(Phone) AS 'PhonesCount' FROM `TestTable` GROUP BY `Phone` HAVING COUNT(Phone) > 1 ORDER BY `PhonesCount` Desc", sqlExpression);
        }

        [TestMethod]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;

            var config = new MysqlQueryConfig(false) { LoadForeign = true, EscapeStrings = true };
            using var query = new Query(Connection, config, TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            var g = config.NewGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM `TestTable` WHERE `Name` = \"Mike\" AND `Date` = @p1 AND `Alias` = \"\\\"Mik\\\";\\'Mik\\'#--\"", sqlExpression);
        }
    }
}
