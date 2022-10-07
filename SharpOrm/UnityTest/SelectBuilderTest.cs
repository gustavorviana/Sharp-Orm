using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using Teste.Utils;

namespace Teste
{
    [TestClass]
    public class SelectBuilderTest : MysqlTableTest
    {
        [TestMethod]
        public void BasicSelect()
        {
            using var query = new Query(connection, TABLE);
            Assert.AreEqual("SELECT * FROM TestTable", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectColumnsName()
        {
            using var query = NewQuery();
            query.Select("Id", "Name");
            Assert.AreEqual("SELECT Id, Name FROM TestTable", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column("Name", "meuNome"));
            Assert.AreEqual("SELECT Id, Name AS meuNome FROM TestTable", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectRawColumn()
        {
            using var query = NewQuery();
            query.Select(new Column("Id"), new Column(new SqlExpression("TOLOWER(Name)"), "meuNome"));
            Assert.AreEqual("SELECT Id, TOLOWER(Name) AS meuNome FROM TestTable", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectWithLimit()
        {
            using var query = new Query(connection, TABLE) { Limit = 10 };
            Assert.AreEqual("SELECT * FROM TestTable LIMIT 10", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectWithOffset()
        {
            using var query = new Query(connection, TABLE) { Offset = 10 };
            Assert.AreEqual("SELECT * FROM TestTable OFFSET 10", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimit()
        {
            using var query = new Query(connection, TABLE) { Offset = 10, Limit = 10 };
            Assert.AreEqual("SELECT * FROM TestTable LIMIT 10 OFFSET 10", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectWithDistinct()
        {
            using var query = new Query(connection, TABLE) { Distinct = true };
            Assert.AreEqual("SELECT DISTINCT * FROM TestTable", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectWithOffsetLimitDistinct()
        {
            using var query = new Query(connection, TABLE) { Offset = 10, Limit = 10, Distinct = true };
            Assert.AreEqual("SELECT DISTINCT * FROM TestTable LIMIT 10 OFFSET 10", Grammar.SelectCommand(query).CommandText);
        }

        [TestMethod]
        public void SelectBasicWhere()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column", "=", "value");
            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @p0", Grammar.SelectCommand(query).CommandText);
            Assert.AreEqual(1, query.GetInfo().Command.Parameters.Count);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = new Query(connection, TABLE) { Limit = 10 };
            query.Where("column", "=", "value");
            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @p0 LIMIT 10", Grammar.SelectCommand(query).CommandText);
            Assert.AreEqual(1, query.GetInfo().Command.Parameters.Count);
        }

        [TestMethod]
        public void SelectWhereCallbackQuery()
        {
            using var query = new Query(connection, TABLE);
            query.Where(e => e.Where("column", "=", "value"));

            Assert.AreEqual("SELECT * FROM TestTable WHERE (column = @p0)", Grammar.SelectCommand(query).CommandText);
            Assert.AreEqual(1, query.GetInfo().Command.Parameters.Count);
        }

        [TestMethod]
        public void SelectMultipleWhere()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column1", "=", "value1");
            query.Where(e => e.Where("column2", "=", "value2"));

            Assert.AreEqual("SELECT * FROM TestTable WHERE column1 = @p0 AND (column2 = @p1)", Grammar.SelectCommand(query).CommandText);
            Assert.AreEqual(2, query.GetInfo().Command.Parameters.Count);
        }

        [TestMethod]
        public void SelectWhereOr()
        {
            using var query = new Query(connection, TABLE);
            query.Where("column", "=", "teste")
                .OrWhere("column", "=", "value");

            Assert.AreEqual("SELECT * FROM TestTable WHERE column = @p0 OR column = @p1", Grammar.SelectCommand(query).CommandText);
            Assert.AreEqual(2, query.GetInfo().Command.Parameters.Count);
        }
    }
}
