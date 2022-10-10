using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using Teste.Utils;

namespace UnityTest
{
    [TestClass]
    public class SelectBuilderTest : MysqlTableTest
    {
        [TestMethod]
        public void BasicSelect()
        {
            using var query = NewQuery();
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable", cmd.CommandText);
        }

        [TestMethod]
        public void SelectColumnsName()
        {
            using var query = NewQuery();
            query.Select("Id", "Name");
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT Id, Name FROM TestTable", cmd.CommandText);
        }

        [TestMethod]
        public void SelectColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column("Name", "meuNome"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT Id, Name AS meuNome FROM TestTable", cmd.CommandText);
        }

        [TestMethod]
        public void SelectRawColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column(new SqlExpression("TOLOWER(Name) AS meuNome")));
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT Id, TOLOWER(Name) AS meuNome FROM TestTable", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithLimit()
        {
            using var query = new Query(connection, TABLE) { Limit = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable LIMIT 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereIn()
        {
            using var query = new Query(connection, TABLE);
            query.Where("id", "IN", new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE id IN (@c1, @c2, @c3, @c4, @c5, @c6, @c7, @c8, @c9)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereInQuery()
        {
            using var query = NewQuery();
            query.Where("id", "IN", this.CreateQueryForWhere());
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE id IN (SELECT Id FROM TestIds WHERE Type = @c1)", cmd.CommandText);
        }

        private Query CreateQueryForWhere()
        {
            return (Query)new Query(connection, "TestIds").Select("Id").Where("Type", "=", "Unity");
        }

        [TestMethod]
        public void SelectWithOffset()
        {
            using var query = new Query(connection, TABLE) { Offset = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimit()
        {
            using var query = new Query(connection, TABLE) { Offset = 10, Limit = 10 };
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable LIMIT 10 OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithDistinct()
        {
            using var query = new Query(connection, TABLE) { Distinct = true };
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT DISTINCT * FROM TestTable", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimitDistinct()
        {
            using var query = new Query(connection, TABLE) { Offset = 10, Limit = 10, Distinct = true };
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT DISTINCT * FROM TestTable LIMIT 10 OFFSET 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectBasicWhere()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @c1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = new Query(connection, TABLE) { Limit = 10 };
            query.Where("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @c1 LIMIT 10", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereCallbackQuery()
        {
            using var query = new Query(connection, TABLE);
            query.Where(e => e.Where("column", "=", "value"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE (column = @c1)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectMultipleWhere()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column1", "=", "value1");
            query.Where(e => e.Where("column2", "=", "value2"));
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column1 = @c1 AND (column2 = @c2)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereOr()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column", "=", "teste")
                .OrWhere("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @c1 OR column = @c2", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereWithColumnPrefixSuffix()
        {
            QueryDefaults.Config.ColumnPrefix = "`";
            QueryDefaults.Config.ColumnSuffix = "`";

            using var query = new Query(connection, TABLE);
            query.Select("Id").Where("column", "=", "value");
            using var g = new MysqlGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT `Id` FROM TestTable WHERE `column` = @c1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereColumnsEquals()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column1", "=", new Column("column2"))
                .Where(new Column("column2"), "=", new Column("column3"));

            using var g = new MysqlGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column1 = column2 AND column2 = column3", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereSqlExpression()
        {
            using var query = new Query(connection, TABLE);
            query.Where(new SqlExpression("column1 = 1"));

            using var g = new MysqlGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column1 = 1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereRawColumn()
        {
            using var query = new Query(connection, TABLE);
            query.Where((Column)"UPPER(column1)", "=", "ABC");

            using var g = new MysqlGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE UPPER(column1) = @c1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereRawValue()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column1", "=", (SqlExpression)"UPPER(column2)");

            using var g = new MysqlGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM TestTable WHERE column1 = UPPER(column2)", cmd.CommandText);
        }
    }
}
